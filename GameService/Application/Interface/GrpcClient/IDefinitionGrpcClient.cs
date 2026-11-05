namespace Application.Interface.GrpcClient
{
    public interface IDefinitionGrpcClient
    {
        Task RequestGameStartupAsync(
            CancellationToken cancellationToken = default);
    }
}