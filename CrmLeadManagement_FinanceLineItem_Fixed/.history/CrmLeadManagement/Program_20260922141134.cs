using CrmLeadManagement.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL connection.
// On Render, set:
// ConnectionStrings__DefaultConnection = <PostgreSQL Internal Database URL>
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured.");

var connectionStringProvider = builder.Configuration is IConfigurationRoot configurationRoot
    ? configurationRoot.Providers
        .Reverse()
        .FirstOrDefault(provider =>
            provider.TryGet("ConnectionStrings:DefaultConnection", out _))
        ?.GetType().Name
    : "Unknown";

// PostgreSQL via EF Core.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));

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

    var postgresConnection =
        new NpgsqlConnectionStringBuilder(connectionString);

    app.Logger.LogInformation(
        "PostgreSQL configuration resolved from {Provider}: Host={Host}, Database={Database}",
        connectionStringProvider,
        postgresConnection.Host,
        postgresConnection.Database);

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
