using CrmLeadManagement.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL via EF Core. Locally this comes from appsettings.json / appsettings.Development.json
// (ConnectionStrings:DefaultConnection). On Render it comes from the
// ConnectionStrings__DefaultConnection environment variable (standard ASP.NET Core env-var
// binding, "__" maps to the ":" section separator), which config picks up automatically —
// no extra wiring needed. ResolvePostgresConnectionString() only normalizes the value if
// Render ever supplies it as a "postgresql://user:pass@host:port/db" URL instead of an
// ADO.NET-style string; a value that's already ADO.NET-style is returned unchanged.
var connectionString = ResolvePostgresConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection"));

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "PostgreSQL connection string 'ConnectionStrings:DefaultConnection' is missing. " +
        "Set the ConnectionStrings__DefaultConnection environment variable on Render.");
}

// Never log the actual connection string (it contains the password) — just confirm it's set.
builder.Logging.AddConsole();
Console.WriteLine("PostgreSQL connection string detected.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Lets the static LeadStore/SettingsStore classes reach the current request's
// AppDbContext without changing every controller's constructor — see DbAccessor.cs.
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
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

// Creates the database/tables from the model if they don't exist yet, then seeds the
// same sample leads/statuses/sources/users the old in-memory version shipped with
// (only if those tables are empty, so this is safe to leave in on every startup).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
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

app.Run();

// Render's managed PostgreSQL "Internal/External Connection String" is sometimes copied in
// as a URL: postgresql://user:password@host:port/database (optionally with a query string
// such as ?sslmode=require). Npgsql needs an ADO.NET-style "Host=...;Username=...;..."
// string, so convert only when the value actually looks like that URL form. Anything already
// in ADO.NET "key=value;key=value" form (including one Render gives you directly, or your own
// local dev string) is returned completely untouched.
static string? ResolvePostgresConnectionString(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return raw;
    }

    if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return raw;
    }

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');
    var port = uri.Port == -1 ? 5432 : uri.Port;

    var builderCs = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = port,
        Username = username,
        Password = password,
        Database = database,
        SslMode = Npgsql.SslMode.Require,
        TrustServerCertificate = true
    };

    return builderCs.ToString();
}
