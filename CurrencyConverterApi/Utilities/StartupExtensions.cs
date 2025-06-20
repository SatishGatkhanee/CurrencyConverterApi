using CurrencyConverterApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using System.Text;
using System.Threading.RateLimiting;

namespace CurrencyConverterApi.Utilities
{
    internal static class StartupExtensions
    {
        public static void AddHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<HttpClients>(configuration.GetSection("AppSettings:apiClients:FrankfurterApi"));
            services.AddHttpClient("FrankfurterApi", (sp, client) =>
             {
                 var options = sp.GetRequiredService<IOptions<HttpClients>>().Value;
                 client.BaseAddress = new Uri(options.BaseUrl);
                 client.Timeout = TimeSpan.FromSeconds(10);
             }).AddPolicyHandler(ResiliencePolicy);
        }

        public static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "JWT Auth header using the Bearer scheme",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };
                c.AddSecurityDefinition("Bearer", securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, Array.Empty<string>() }
                });
            });
            services.ConfigureOptions<ConfigureSwaggerOptions>();
        }

        public static void ConfigureAuthetication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection("Jwt");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero
                };

                opts.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"JWT auth failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"JWT validated for user: {context.Principal!.Identity?.Name}");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        public static void ConfigureOpenTelemetry(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CurrencyAPI"))
                        .AddOtlpExporter(opts =>
                        {
                            opts.Endpoint = new Uri("http://localhost:4317"); // OTLP collector
                        });
                });
        }

        public static void ConfigureRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("fixed", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 5; // Max 5 requests
                    limiterOptions.Window = TimeSpan.FromSeconds(10); // Per 10 seconds
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 2;
                });
            });
        }

        private static IAsyncPolicy<HttpResponseMessage> ResiliencePolicy
        {
            get
            {
                var retry = HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt =>
                            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds:N1}s due to {outcome.Exception?.Message
                                ?? outcome.Result.StatusCode.ToString()}");
                        });

                var circuitBreaker = HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: 3,
                        durationOfBreak: TimeSpan.FromSeconds(30),
                        onBreak: (result, span) => Console.WriteLine("Circuit broken"),
                        onReset: () => Console.WriteLine("Circuit reset"),
                        onHalfOpen: () => Console.WriteLine("Circuit in half-open state")
                    );

                return Policy.WrapAsync(retry, circuitBreaker);
            }
        }
    }
}
