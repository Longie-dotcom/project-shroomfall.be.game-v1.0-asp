using Application.Interface.GrpcClient;
using Contract.Grpc.Version;

namespace Infrastructure.GrpcClient
{
    public class VersionGrpcClient : IVersionGrpcClient
    {
        #region Attributes
        private readonly VersionService.VersionServiceClient client;
        #endregion

        #region Properties
        #endregion

        public VersionGrpcClient(
            VersionService.VersionServiceClient client)
        {
            this.client = client;
        }

        #region Methods
        public async Task<long> RequestVersioningAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await client.RequestVersionCacheAsync(
                new VersionCacheRequest(),
                cancellationToken: cancellationToken);

            return result.Version;
        }
        #endregion
    }
}