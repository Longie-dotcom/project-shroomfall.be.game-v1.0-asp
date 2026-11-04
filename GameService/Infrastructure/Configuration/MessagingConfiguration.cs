using Domain.DomainException;
using Infrastructure.Messaging.Consumer;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using ResponseCode;

namespace Infrastructure.Configuration
{
    public static class MessagingConfiguration
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        #region Methods
        public static IServiceCollection AddMessagingConfiguration(
            this IServiceCollection services)
        {
            // RABBITMQ
            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
            if (string.IsNullOrWhiteSpace(rabbitHost))
                throw new InternalException(
                    InfrastructureCode.MessagingConfigurationCode.RabbitMqHostMissing,
                    "Critical infrastructure configuration missing. Environment variable 'RABBITMQ_HOST' was not found.");

            var rabbitUsername = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER");
            if (string.IsNullOrWhiteSpace(rabbitUsername))
                throw new InternalException(
                    InfrastructureCode.MessagingConfigurationCode.RabbitMqUsernameMissing,
                    "Critical infrastructure configuration missing. Environment variable 'RABBITMQ_DEFAULT_USER' was not found.");

            var rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS");
            if (string.IsNullOrWhiteSpace(rabbitPassword))
                throw new InternalException(
                    InfrastructureCode.MessagingConfigurationCode.RabbitMqPasswordMissing,
                    "Critical infrastructure configuration missing. Environment variable 'RABBITMQ_DEFAULT_PASS' was not found.");

            services.AddMassTransit(configurator =>
            {
                // CONSUMER
                configurator.AddConsumer<DefinitionCacheConsumer>();
                configurator.AddConsumer<GameStartupConsumer>();

                configurator.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitHost, "/", host =>
                    {
                        host.Username(rabbitUsername);
                        host.Password(rabbitPassword);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
        #endregion
    }
}