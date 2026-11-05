using Application.Interface.GrpcClient;
using Contract.Grpc.Management;
using Contract.Grpc.Version;
using Domain.DomainException;
using Infrastructure.GrpcClient;
using Microsoft.Extensions.DependencyInjection;
using ResponseCode;

namespace Infrastructure.Configuration
{
    public static class GrpcConfiguration
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        #region Methods
        public static IServiceCollection AddGrpcConfiguration(
            this IServiceCollection services)
        {
            // GRPC
            var managementGrpcUrl = Environment.GetEnvironmentVariable("MANAGEMENT_GRPC_URL");
            if (string.IsNullOrWhiteSpace(managementGrpcUrl))
                throw new InternalException(
                    InfrastructureCode.GrpcConfigurationCode.ManagementGrpcUrlMissing,
                    "Critical infrastructure configuration missing. Environment variable 'MANAGEMENT_GRPC_URL' was not found.");

            services.AddGrpcClient<DefinitionService.DefinitionServiceClient>(
                options => { options.Address = new Uri(managementGrpcUrl); });
            services.AddGrpcClient<VersionService.VersionServiceClient>(
                options => { options.Address = new Uri(managementGrpcUrl); });

            // CLIENT
            services.AddScoped<IDefinitionGrpcClient, DefinitionGrpcClient>();
            services.AddScoped<IVersionGrpcClient, VersionGrpcClient>();

            return services;
        }
        #endregion
    }
}