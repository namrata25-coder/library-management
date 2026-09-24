using CrmLeadManagement.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// SQL Server via EF Core. Connection string lives in appsettings.json
// (ConnectionStrings:DefaultConnection) — point it at your local SQL Server instance.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

var connectionStringProvider = builder.Configuration is IConfigurationRoot configurationRoot
    ? configurationRoot.Providers
        .Reverse()
        .FirstOrDefault(provider => provider.TryGet("ConnectionStrings:DefaultConnection", out _))
        ?.GetType().Name
    : "Unknown";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Lets the static LeadStore/SettingsStore classes reach the current request's
// AppDbContext without changing every controller's constructor — see DbAccessor.cs.
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Keep API responses readable (PascalCase already matches the model properties,
        // this just avoids surprises if a client sends camelCase — it's accepted either way).
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Allows a separate frontend (React/Angular/mobile app running on a different origin)
// to call this API during development. Tighten this before deploying to production.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Swagger / OpenAPI. Only the API Controllers under Controllers/Api are annotated with
// [ApiController], so Swashbuckle picks up their routes/models automatically; MVC
// controllers (Home, Lead, Settings) that return Views are unaffected.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CRM Lead Management API",
        Version = "v1",
        Description = "JSON REST API for the Leads and Settings modules of the CRM Lead Management application."
    });

    // Guards against "Conflicting schemaId" exceptions (the most common cause of a 500 on
    // /swagger/v1/swagger.json) if any DTOs ever end up with the same class name in
    // different namespaces — uses the full namespace-qualified name instead of just the
    // short type name when generating schema ids.
    options.CustomSchemaIds(type => type.FullName);

    // Pull in the XML doc comments (see GenerateDocumentationFile in the .csproj) so
    // controller/action summaries and DTO property comments show up in Swagger UI.
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

// Applies the versioned EF Core schema to the configured SQL Server database, then
// seeds the same sample leads/statuses/sources/users the old in-memory version shipped
// with (only if those tables are empty, so this is safe to leave in on every startup).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    db.Database.Migrate();
    LeadStore.SeedIfEmpty(db);
    SettingsStore.SeedIfEmpty(db);
}

// Persists any changes made to tracked entities during the request (e.g. LeadController's
// AddCall/AddFollowUp/AssignLead actions mutate a lead returned by LeadStore.GetById without
// an explicit "Save" call) — this commits them once the request finishes.
//
// IMPORTANT: by the time next() returns, MVC/Web API has already written the response
// (status line + headers, and for a chunked response, the first body chunk) to the socket.
// If SaveChangesAsync() then throws here, we can no longer send a clean 4xx/5xx — the 200
// OK and part of the JSON body are already on the wire, so the runtime can only abort the
// connection without ever sending the terminating chunk. That is exactly what produced
// "net::ERR_INCOMPLETE_CHUNKED_ENCODING 200 (OK)" / "TypeError: Failed to fetch" in the
// browser for the Estimation API: the underlying SqlException ("Invalid object name
// 'FinanceLineItem'") was real, but by the time it reached here it was too late to report
// it cleanly.
//
// The JSON API controllers (Controllers/Api/*) now call SaveChangesAsync() themselves,
// inside a try/catch, BEFORE returning a result — see LeadsApiController — so for those
// routes this middleware's SaveChangesAsync() below is a harmless no-op (nothing left to
// save). It stays, wrapped in try/catch, purely as a safety net for the MVC controllers
// (LeadController, SettingsController) that still rely on it.
app.Use(async (context, next) =>
{
    await next();
    try
    {
        var db = context.RequestServices.GetRequiredService<AppDbContext>();
        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("TrailingSaveChanges");
        logger.LogError(ex, "SaveChangesAsync failed after the response for {Method} {Path} had already started.",
            context.Request.Method, context.Request.Path);
        // The response has very likely already started, so we can't rewrite the status
        // code or body here — logging is the best we can do. Endpoints that matter (the
        // JSON API) avoid ever reaching this situation by saving before they return.
    }
});

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
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
