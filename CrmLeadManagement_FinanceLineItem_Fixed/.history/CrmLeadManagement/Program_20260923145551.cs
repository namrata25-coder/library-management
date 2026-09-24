using CrmLeadManagement.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();
var isPostgreSql = !isDevelopment;
var connectionString = isDevelopment
    ? @"Server=(localdb)\MSSQLLocalDB;Database=CrmLeadManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
    : builder.Configuration.GetConnectionString("DefaultConnection")   
        ?? string.Empty;

var connectionStringProvider = isDevelopment
    ? "Development LocalDB"
    : "ConnectionStrings__DefaultConnection environment variable";

if (isPostgreSql)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Production requires a PostgreSQL connection string in the "
            + "ConnectionStrings__DefaultConnection environment variable.");
    }

    var sqlServerOptions = new[]
    {
        "Trusted_Connection",
        "TrustServerCertificate",
        "Integrated Security",
        "IntegratedSecurity"
    };

    if (sqlServerOptions.Any(option =>
        connectionString.Contains(option, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException(
            "ConnectionStrings__DefaultConnection contains SQL Server options. "
            + "Production requires a PostgreSQL connection string without Trusted_Connection, "
            + "TrustServerCertificate, or Integrated Security.");
    }

    try
    {
        _ = new NpgsqlConnectionStringBuilder(connectionString);
    }
    catch (ArgumentException ex)
    {
        throw new InvalidOperationException(
            "ConnectionStrings__DefaultConnection is not a valid PostgreSQL connection string. "
            + "Do not include SQL Server options such as Trusted_Connection, "
            + "TrustServerCertificate, or Integrated Security.",
            ex);
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isPostgreSql)
    {
        options.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
    }
    else
    {
        options.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
    }
});

// Allows static stores to access the current request's DbContext.
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "CRM Lead Management API",
            Version = "v1",
            Description =
                "JSON REST API for the Leads and Settings modules of the CRM Lead Management application."
        });

    options.CustomSchemaIds(type => type.FullName);

    var xmlFile =
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath = Path.Combine(
        AppContext.BaseDirectory,
        xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Initialize DbAccessor.
DbAccessor.Initialize(
    app.Services.GetRequiredService<IHttpContextAccessor>());

// Apply EF Core migrations and seed initial data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    app.Logger.LogInformation(
        "{DatabaseProvider} configuration resolved from {Provider}",
        isPostgreSql ? "PostgreSQL" : "SQL Server",
        connectionStringProvider);

    db.Database.Migrate();

    LeadStore.SeedIfEmpty(db);
    SettingsStore.SeedIfEmpty(db);
}

// Safety-net save for MVC requests.
app.Use(async (context, next) =>
{
    await next();

    try
    {
        var db =
            context.RequestServices.GetRequiredService<AppDbContext>();

        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        var logger =
            context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("TrailingSaveChanges");

        logger.LogError(
            ex,
            "SaveChangesAsync failed after the response for {Method} {Path} had already started.",
            context.Request.Method,
            context.Request.Path);
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "CRM Lead Management API v1");
    });
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
