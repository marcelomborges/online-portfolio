using OnlinePortfolio.Api.Data;
using OnlinePortfolio.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, _, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddApplicationIdentity();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Online Portfolio API", Version = "v1" });
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

var swaggerEnabled = app.Configuration.GetValue<bool?>("Swagger:Enabled")
    ?? app.Environment.IsDevelopment();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.RoutePrefix = "swagger");
}

app.MapHealthChecks("/health");
app.MapControllers();

try
{
    Log.Information("Starting OnlinePortfolio.Api");
    await IdentityRoleSeeder.SeedAsync(app.Services);
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
