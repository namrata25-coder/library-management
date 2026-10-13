using CrmLeadManagement.Models;
using CrmLeadManagement.Models.Dtos;
using CrmLeadManagement.Filters;
using CrmLeadManagement.Security;
using CrmLeadManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// SQL Server via EF Core. Connection string lives in appsettings.json
// (ConnectionStrings:DefaultConnection) — point it at your SSMS instance.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Lets the static LeadStore/SettingsStore classes reach the current request's
// AppDbContext without changing every controller's constructor — see DbAccessor.cs.
builder.Services.AddHttpContextAccessor();

// Authentication: Cookie scheme for browser sessions + Bearer scheme for API/mobile
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.Name = "NimbusCRM.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddScheme<AuthenticationSchemeOptions, BearerTokenAuthenticationHandler>("Bearer", null);

// Cleanup expired/revoked access tokens older than 7 days daily
builder.Services.AddHostedService<TokenCleanupBackgroundService>();

// Rate limiting for auth endpoints (10 requests/minute per IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var error = new ApiError { Message = "Too many requests. Please try again later." };
        await context.HttpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(error), cancellationToken: token);
    };
    options.AddPolicy("AuthRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddControllersWithViews(options =>
    {
        // Saves tracked changes before the response starts (see Filters/SaveChangesFilter.cs).
        options.Filters.Add<SaveChangesFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Keep API responses readable (PascalCase already matches the model properties,
        // this just avoids surprises if a client sends camelCase — it's accepted either way).
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// CORS: named policy from Cors:AllowedOrigins outside Development (AllowAll only in Development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// Swagger / OpenAPI with Bearer authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CRM Lead Management API",
        Version = "v1",
        Description = "JSON REST API for the Leads, Settings, and Auth modules with Bearer token authentication."
    });

    // Add Bearer Http security definition (user enters only the token)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your opaque bearer token (without the 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.CustomSchemaIds(type => type.FullName);

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Wire up DbAccessor so LeadStore/SettingsStore can find the current request's DbContext.
DbAccessor.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>());

// Applies pending EF Core migrations on startup, then seeds initial data if tables are empty.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

    try
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception migEx)
        {
            startupLogger.LogCritical(migEx,
                "Database migrations failed. Application startup is stopping to avoid running with an outdated schema.");
            throw;
        }

        // Ensure Work Management tables exist in case migration was already partially applied or unversioned
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'WorkProjects', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkProjects] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectCode] nvarchar(max) NOT NULL,
                    [ProjectName] nvarchar(max) NOT NULL,
                    [ClientName] nvarchar(max) NOT NULL,
                    [ClientCompany] nvarchar(max) NOT NULL,
                    [ProjectType] nvarchar(max) NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [StartDate] nvarchar(max) NOT NULL,
                    [Deadline] nvarchar(max) NOT NULL,
                    [ProjectManager] nvarchar(max) NOT NULL,
                    [AssignedMembers] nvarchar(max) NOT NULL,
                    [Budget] decimal(18,2) NOT NULL,
                    [Priority] nvarchar(max) NOT NULL,
                    [Status] nvarchar(max) NOT NULL,
                    [Notes] nvarchar(max) NOT NULL,
                    [CreatedDate] nvarchar(max) NOT NULL,
                    CONSTRAINT [PK_WorkProjects] PRIMARY KEY ([Id])
                );
            END;

            IF OBJECT_ID(N'WorkMilestones', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkMilestones] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [MilestoneCode] nvarchar(max) NOT NULL,
                    [MilestoneName] nvarchar(max) NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [StartDate] nvarchar(max) NOT NULL,
                    [DueDate] nvarchar(max) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [CompletedAmount] decimal(18,2) NOT NULL,
                    [AssignedMembers] nvarchar(max) NOT NULL,
                    [Priority] nvarchar(max) NOT NULL,
                    [Status] nvarchar(max) NOT NULL,
                    [Notes] nvarchar(max) NOT NULL,
                    CONSTRAINT [PK_WorkMilestones] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkMilestones_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'WorkTasks', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkTasks] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [MilestoneId] int NULL,
                    [TaskCode] nvarchar(max) NOT NULL,
                    [TaskName] nvarchar(max) NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [AssignedMember] nvarchar(max) NOT NULL,
                    [StartDate] nvarchar(max) NOT NULL,
                    [DueDate] nvarchar(max) NOT NULL,
                    [TaskAmount] decimal(18,2) NOT NULL,
                    [CompletedAmount] decimal(18,2) NOT NULL,
                    [Priority] nvarchar(max) NOT NULL,
                    [Status] nvarchar(max) NOT NULL,
                    [Notes] nvarchar(max) NOT NULL,
                    [CreatedDate] nvarchar(max) NOT NULL,
                    CONSTRAINT [PK_WorkTasks] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkTasks_WorkMilestones_MilestoneId] FOREIGN KEY ([MilestoneId]) REFERENCES [WorkMilestones] ([Id]),
                    CONSTRAINT [FK_WorkTasks_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'WorkInvoices', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkInvoices] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [MilestoneId] int NULL,
                    [InvoiceNumber] nvarchar(max) NOT NULL,
                    [ClientName] nvarchar(max) NULL,
                    [ClientCompany] nvarchar(max) NULL,
                    [InvoiceDate] nvarchar(max) NOT NULL,
                    [DueDate] nvarchar(max) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [TaxRate] decimal(18,2) NOT NULL DEFAULT 0,
                    [Tax] decimal(18,2) NOT NULL,
                    [Discount] decimal(18,2) NOT NULL,
                    [GrandTotal] decimal(18,2) NOT NULL,
                    [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0,
                    [PaymentStatus] nvarchar(max) NOT NULL,
                    [PaymentDate] nvarchar(max) NOT NULL,
                    [IsRecurring] bit NOT NULL DEFAULT 0,
                    [RecurringFrequency] nvarchar(max) NULL,
                    [Notes] nvarchar(max) NOT NULL,
                    [CreatedDate] nvarchar(max) NULL,
                    CONSTRAINT [PK_WorkInvoices] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkInvoices_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END
            ELSE
            BEGIN
                IF COL_LENGTH('WorkInvoices', 'MilestoneId') IS NULL
                    ALTER TABLE WorkInvoices ADD MilestoneId int NULL;
                IF COL_LENGTH('WorkInvoices', 'ClientName') IS NULL
                    ALTER TABLE WorkInvoices ADD ClientName nvarchar(max) NULL;
                IF COL_LENGTH('WorkInvoices', 'ClientCompany') IS NULL
                    ALTER TABLE WorkInvoices ADD ClientCompany nvarchar(max) NULL;
                IF COL_LENGTH('WorkInvoices', 'TaxRate') IS NULL
                    ALTER TABLE WorkInvoices ADD TaxRate decimal(18,2) NOT NULL DEFAULT 0;
                IF COL_LENGTH('WorkInvoices', 'PaidAmount') IS NULL
                    ALTER TABLE WorkInvoices ADD PaidAmount decimal(18,2) NOT NULL DEFAULT 0;
                IF COL_LENGTH('WorkInvoices', 'IsRecurring') IS NULL
                    ALTER TABLE WorkInvoices ADD IsRecurring bit NOT NULL DEFAULT 0;
                IF COL_LENGTH('WorkInvoices', 'RecurringFrequency') IS NULL
                    ALTER TABLE WorkInvoices ADD RecurringFrequency nvarchar(max) NULL;
                IF COL_LENGTH('WorkInvoices', 'CreatedDate') IS NULL
                    ALTER TABLE WorkInvoices ADD CreatedDate nvarchar(max) NULL;
            END;

            IF OBJECT_ID(N'WorkPayments', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkPayments] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [InvoiceId] int NOT NULL,
                    [PaymentNumber] nvarchar(max) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [PaymentDate] nvarchar(max) NOT NULL,
                    [PaymentMethod] nvarchar(max) NOT NULL,
                    [TransactionReference] nvarchar(max) NULL,
                    [Notes] nvarchar(max) NULL,
                    [CreatedDate] nvarchar(max) NULL,
                    CONSTRAINT [PK_WorkPayments] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkPayments_WorkInvoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [WorkInvoices] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'WorkRecurringInvoices', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkRecurringInvoices] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [MilestoneId] int NULL,
                    [ProfileNumber] nvarchar(max) NOT NULL,
                    [Title] nvarchar(max) NOT NULL,
                    [ClientName] nvarchar(max) NULL,
                    [ClientCompany] nvarchar(max) NULL,
                    [Frequency] nvarchar(max) NOT NULL,
                    [StartDate] nvarchar(max) NOT NULL,
                    [NextInvoiceDate] nvarchar(max) NOT NULL,
                    [EndDate] nvarchar(max) NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [TaxRate] decimal(18,2) NOT NULL,
                    [Tax] decimal(18,2) NOT NULL,
                    [Discount] decimal(18,2) NOT NULL,
                    [GrandTotal] decimal(18,2) NOT NULL,
                    [Status] nvarchar(max) NOT NULL,
                    [TotalCycles] int NOT NULL,
                    [CompletedCycles] int NOT NULL,
                    [Notes] nvarchar(max) NULL,
                    [CreatedDate] nvarchar(max) NULL,
                    CONSTRAINT [PK_WorkRecurringInvoices] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkRecurringInvoices_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'WorkFiles', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkFiles] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [FileName] nvarchar(max) NULL,
                    [OriginalFileName] nvarchar(max) NULL,
                    [FilePath] nvarchar(max) NULL,
                    [FileSizeBytes] bigint NOT NULL,
                    [FileType] nvarchar(max) NULL,
                    [UploadedBy] nvarchar(max) NULL,
                    [UploadedDate] nvarchar(max) NULL,
                    [Description] nvarchar(max) NULL,
                    CONSTRAINT [PK_WorkFiles] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkFiles_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'WorkTimesheets', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkTimesheets] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [TaskId] int NULL,
                    [MemberName] nvarchar(max) NULL,
                    [LogDate] nvarchar(max) NULL,
                    [HoursWorked] decimal(18,2) NOT NULL,
                    [StartTime] nvarchar(max) NULL,
                    [EndTime] nvarchar(max) NULL,
                    [HourlyRate] decimal(18,2) NOT NULL,
                    [TotalCost] decimal(18,2) NOT NULL,
                    [BillingStatus] nvarchar(max) NULL,
                    [Description] nvarchar(max) NULL,
                    [Notes] nvarchar(max) NULL,
                    [CreatedDate] nvarchar(max) NULL,
                    CONSTRAINT [PK_WorkTimesheets] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkTimesheets_WorkTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [WorkTasks] ([Id]),
                    CONSTRAINT [FK_WorkTimesheets_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'WorkExpenses', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkExpenses] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [ExpenseCode] nvarchar(max) NULL,
                    [ExpenseName] nvarchar(max) NULL,
                    [Category] nvarchar(max) NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [TaxAmount] decimal(18,2) NOT NULL,
                    [TotalAmount] decimal(18,2) NOT NULL,
                    [ExpenseDate] nvarchar(max) NULL,
                    [PaymentMethod] nvarchar(max) NULL,
                    [Status] nvarchar(max) NULL,
                    [PaidBy] nvarchar(max) NULL,
                    [ReceiptFileName] nvarchar(max) NULL,
                    [ReceiptFilePath] nvarchar(max) NULL,
                    [Notes] nvarchar(max) NULL,
                    [CreatedDate] nvarchar(max) NULL,
                    CONSTRAINT [PK_WorkExpenses] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkExpenses_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'WorkNotes', N'U') IS NULL
            BEGIN
                CREATE TABLE [WorkNotes] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProjectId] int NOT NULL,
                    [Title] nvarchar(max) NULL,
                    [Content] nvarchar(max) NULL,
                    [Category] nvarchar(max) NULL,
                    [IsPinned] bit NOT NULL,
                    [ColorTag] nvarchar(max) NULL,
                    [CreatedBy] nvarchar(max) NULL,
                    [CreatedDate] nvarchar(max) NULL,
                    [LastModifiedDate] nvarchar(max) NULL,
                    CONSTRAINT [PK_WorkNotes] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkNotes_WorkProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'TaxMasters', N'U') IS NULL
            BEGIN
                CREATE TABLE [TaxMasters] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [TaxName] nvarchar(max) NOT NULL,
                    [TaxType] nvarchar(max) NOT NULL,
                    [TaxRate] decimal(18,2) NOT NULL,
                    [Description] nvarchar(max) NULL,
                    [Status] nvarchar(max) NOT NULL,
                    CONSTRAINT [PK_TaxMasters] PRIMARY KEY ([Id])
                );
            END;

            IF COL_LENGTH('Users', 'ModulePermissions') IS NULL
                ALTER TABLE [Users] ADD [ModulePermissions] nvarchar(max) NULL;

            IF COL_LENGTH('WorkInvoices', 'TaxName') IS NULL
                ALTER TABLE [WorkInvoices] ADD [TaxName] nvarchar(max) NULL;
            IF COL_LENGTH('WorkInvoices', 'TaxType') IS NULL
                ALTER TABLE [WorkInvoices] ADD [TaxType] nvarchar(max) NULL;

            IF COL_LENGTH('WorkRecurringInvoices', 'TaxName') IS NULL
                ALTER TABLE [WorkRecurringInvoices] ADD [TaxName] nvarchar(max) NULL;
            IF COL_LENGTH('WorkRecurringInvoices', 'TaxType') IS NULL
                ALTER TABLE [WorkRecurringInvoices] ADD [TaxType] nvarchar(max) NULL;

            IF COL_LENGTH('Estimations', 'TaxRate') IS NULL
                ALTER TABLE [Estimations] ADD [TaxRate] nvarchar(max) NULL;
            IF COL_LENGTH('Estimations', 'TaxName') IS NULL
                ALTER TABLE [Estimations] ADD [TaxName] nvarchar(max) NULL;
            IF COL_LENGTH('Estimations', 'TaxType') IS NULL
                ALTER TABLE [Estimations] ADD [TaxType] nvarchar(max) NULL;

            IF COL_LENGTH('Quotations', 'TaxRate') IS NULL
                ALTER TABLE [Quotations] ADD [TaxRate] nvarchar(max) NULL;
            IF COL_LENGTH('Quotations', 'TaxName') IS NULL
                ALTER TABLE [Quotations] ADD [TaxName] nvarchar(max) NULL;
            IF COL_LENGTH('Quotations', 'TaxType') IS NULL
                ALTER TABLE [Quotations] ADD [TaxType] nvarchar(max) NULL;

            IF COL_LENGTH('Proposals', 'Subtotal') IS NULL
                ALTER TABLE [Proposals] ADD [Subtotal] nvarchar(max) NULL;
            IF COL_LENGTH('Proposals', 'Discount') IS NULL
                ALTER TABLE [Proposals] ADD [Discount] nvarchar(max) NULL;
            IF COL_LENGTH('Proposals', 'TaxRate') IS NULL
                ALTER TABLE [Proposals] ADD [TaxRate] nvarchar(max) NULL;
            IF COL_LENGTH('Proposals', 'Tax') IS NULL
                ALTER TABLE [Proposals] ADD [Tax] nvarchar(max) NULL;
            IF COL_LENGTH('Proposals', 'GrandTotal') IS NULL
                ALTER TABLE [Proposals] ADD [TaxName] nvarchar(max) NULL;
            IF COL_LENGTH('Proposals', 'TaxType') IS NULL
                ALTER TABLE [Proposals] ADD [TaxType] nvarchar(max) NULL;

            IF OBJECT_ID(N'RoleMasters', N'U') IS NULL
            BEGIN
                CREATE TABLE [RoleMasters] (
                    [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                    [RoleName] nvarchar(100) NOT NULL,
                    [Description] nvarchar(max) NULL,
                    [IsSystem] bit NOT NULL DEFAULT 0,
                    [DataAccess] nvarchar(50) NOT NULL DEFAULT 'OnlyAssigned',
                    [PermissionsJson] nvarchar(max) NOT NULL
                );
                CREATE UNIQUE INDEX [IX_RoleMasters_RoleName] ON [RoleMasters] ([RoleName]);
            END

            IF OBJECT_ID(N'AccessTokens', N'U') IS NULL
            BEGIN
                CREATE TABLE [AccessTokens] (
                    [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
                    [UserId] int NOT NULL,
                    [TokenHash] nvarchar(64) NOT NULL,
                    [DeviceId] nvarchar(max) NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [ExpiresAt] datetime2 NOT NULL,
                    [LastUsedAt] datetime2 NULL,
                    [RevokedAt] datetime2 NULL,
                    CONSTRAINT [FK_AccessTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_AccessTokens_TokenHash] ON [AccessTokens] ([TokenHash]);
            END

            IF COL_LENGTH('Users', 'PasswordHash') IS NULL
                ALTER TABLE [Users] ADD [PasswordHash] nvarchar(max) NULL;
            IF COL_LENGTH('Users', 'FailedLoginCount') IS NULL
                ALTER TABLE [Users] ADD [FailedLoginCount] int NOT NULL DEFAULT 0;
            IF COL_LENGTH('Users', 'LockoutUntil') IS NULL
                ALTER TABLE [Users] ADD [LockoutUntil] datetime2 NULL;
            IF COL_LENGTH('Users', 'LastLoginAt') IS NULL
                ALTER TABLE [Users] ADD [LastLoginAt] datetime2 NULL;
        ");

        LeadStore.SeedIfEmpty(db);
        SettingsStore.SeedIfEmpty(db, builder.Configuration);
        RoleStore.SeedIfEmpty(db);
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(ex,
            "Could not initialise the database. Check ConnectionStrings:DefaultConnection in appsettings.json, " +
            "that SQL Server (e.g. SQLEXPRESS) is running, and resolve any migration-reported data conflicts.");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Swagger UI + swagger.json are only exposed in Development, matching the
    // existing exception-page behavior above.
    app.UseSwagger(); // serves /swagger/v1/swagger.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CRM Lead Management API v1");
    }); // serves the interactive UI at /swagger
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "ConfiguredOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Auth", action = "Login" });

app.MapControllerRoute(
    name: "logout",
    pattern: "logout",
    defaults: new { controller = "Auth", action = "Logout" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
