using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AspNetCoreRateLimit;
using CurrencyConverterApi.Middleware;
using CurrencyConverterApi.Models;
using CurrencyConverterApi.Providers;
using CurrencyConverterApi.Services;
using CurrencyConverterApi.Services.Interfaces;
using CurrencyConverterApi.Utilities;
using Serilog;
using Serilog.Context;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

var isAuthEnabled = builder.Configuration.GetValue<bool>("Jwt:IsAuthEnabled");

// Add services to the container.

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.ConfigureSwagger();
builder.Services.ConfigureAuthetication(builder.Configuration);
builder.Services.AddMemoryCache();

builder.Services.ConfigureOpenTelemetry();

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

builder.Services.AddHttpClient();
builder.Services.AddHttpClients(builder.Configuration);
builder.Services.ConfigureRateLimiter();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddScoped<ICurrencyConverterService, CurrencyConverterService>();
builder.Services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddTransient<ICurrencyProvider, FrankfurterProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var desc in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName.ToUpperInvariant());
    }
});

app.Use(async (context, next) =>
{
    var correlationId = context.TraceIdentifier;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    LogContext.PushProperty("CorrelationId", correlationId);
    await next();
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseIpRateLimiting();

if (app.Environment.IsDevelopment())
{
    if (isAuthEnabled)
    {
        app.MapControllers().RequireAuthorization().RequireRateLimiting("fixed");
    }
    else
    {
        app.MapControllers(); // auth disabled for local dev
    }
}
else
{
    app.MapControllers().RequireAuthorization().RequireRateLimiting("fixed");
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.Run();
