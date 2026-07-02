using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using OnlinePortfolio.Api.Data;
using OnlinePortfolio.Api.Infrastructure;
using OnlinePortfolio.Api.Middleware;
using OnlinePortfolio.Api.Options;
using OnlinePortfolio.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, _, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.Configure<DevSeedOptions>(builder.Configuration.GetSection(DevSeedOptions.Section));
builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddApplicationIdentity();
builder.Services.AddApplicationJwt(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Online Portfolio API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "JWT Bearer token. Example: **Bearer {token}**",
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), [] },
    });
});
builder.Services.AddApplicationRateLimiter();  // ApplicationRateLimiterExtensions
builder.Services.AddHostedService<StartupSeederBackgroundService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Trust the reverse proxy (Render) to forward the real client IP and HTTPS scheme.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear default networks/proxies to accept forwarded headers from Render's proxy.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();  // Must be first — resolves real client IP before rate limiter.
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

var swaggerEnabled = app.Configuration.GetValue<bool?>("Swagger:Enabled")
    ?? app.Environment.IsDevelopment();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.RoutePrefix = "swagger");
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

try
{
    Log.Information("Starting OnlinePortfolio.Api");
    // Seeding runs in StartupSeederBackgroundService after Kestrel binds the port.
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
