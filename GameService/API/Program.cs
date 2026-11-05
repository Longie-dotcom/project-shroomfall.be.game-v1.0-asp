using API.Middleware;
using Application;
using Application.Interface.GrpcClient;
using Infrastructure;
using Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ─────────────────────────────
            // LOGGING
            // ─────────────────────────────
            builder.Services.AddLogging();

            // ─────────────────────────────
            // SERVICES
            // ─────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure();

            // ─────────────────────────────
            // CORS
            // ─────────────────────────────
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("StudioCorsPolicy", policy =>
                {
                    policy.SetIsOriginAllowed(origin => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // ─────────────────────────────
            // SWAGGER
            // ─────────────────────────────
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                        return new[] { api.GroupName };

                    var controller = api.ActionDescriptor.RouteValues["controller"];
                    return new[] { controller! };
                });
            });

            // ─────────────────────────────
            // JWT AUTH
            // ─────────────────────────────
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")!;
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")!;
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!;

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = issuer,
                        ValidAudience = audience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)
                        ),

                        ClockSkew = TimeSpan.FromSeconds(15)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/hubs/game") ||
                                 path.StartsWithSegments("/hubs/admin")))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // ─────────────────────────────
            // BUILD
            // ─────────────────────────────
            var app = builder.Build();

            // ─────────────────────────────
            // MIDDLEWARE PIPELINE
            // ─────────────────────────────
            app.UseMiddleware<GlobalExceptionHandler>();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("StudioCorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            // ─────────────────────────────
            // SIGNALR HUB
            // ─────────────────────────────
            app.MapHub<GameHub>("/hubs/game");
            app.MapHub<AdminHub>("/hubs/admin");

            // ─────────────────────────────
            // HTTP API
            // ─────────────────────────────
            app.MapControllers();

            // ─────────────────────────────
            // STARTUP
            // ─────────────────────────────
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    // Adding a tiny delay guarantees the network binding is finished in RabbitMQ
                    await Task.Delay(3000);

                    using (var scope = app.Services.CreateScope())
                    {
                        var definitionGrpcClient = scope.ServiceProvider.GetRequiredService<IDefinitionGrpcClient>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                        logger.LogInformation("Requesting caching definition/meta data...");
                        try
                        {
                            await definitionGrpcClient.RequestGameStartupAsync();
                            logger.LogInformation("Requesting completed.");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to connect to Management Service.");
                        }
                    }
                });
            });

            app.Run();
        }
    }
}