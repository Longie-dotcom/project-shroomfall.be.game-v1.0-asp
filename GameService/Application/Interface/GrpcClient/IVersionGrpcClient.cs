namespace Application.Interface.GrpcClient
{
    public interface IVersionGrpcClient
    {
        Task<long> RequestVersioningAsync(
            CancellationToken cancellationToken = default);
    }
}
