using Application.Interface.GrpcClient;
using Contract.Grpc.Management;

namespace Infrastructure.GrpcClient
{
    public class DefinitionGrpcClient : IDefinitionGrpcClient
    {
        #region Attributes
        private readonly DefinitionService.DefinitionServiceClient client;
        #endregion

        #region Properties
        #endregion

        public DefinitionGrpcClient(
            DefinitionService.DefinitionServiceClient client)
        {
            this.client = client;
        }

        #region Methods
        public async Task RequestGameStartupAsync(
            CancellationToken cancellationToken = default)
        {
            await client.RequestDefinitionCacheAsync(
                new DefinitionCacheRequest(),
                cancellationToken: cancellationToken);
        }
        #endregion
    }
}