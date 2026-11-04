using Application.Interface.Cache;
using Application.Service.WorldService;
using Contract.DTO.Messaging;
using DnsClient.Internal;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Consumer
{
    public class GameStartupConsumer : IConsumer<GameStartupDTO>
    {
        #region Attributes
        private readonly ILogger<GameStartupConsumer> logger;
        private readonly ICacheProvider cacheProvider;
        private readonly BootstrapService bootstrapService;
        #endregion

        #region Properties
        #endregion

        public GameStartupConsumer(
            ILogger<GameStartupConsumer> logger,
            ICacheProvider cacheProvider,
            BootstrapService bootstrapService)
        {
            this.logger = logger;
            this.cacheProvider = cacheProvider;
            this.bootstrapService = bootstrapService;
        }

        #region Methods
        public async Task Consume(
            ConsumeContext<GameStartupDTO> context)
        {
            var message = context.Message;

            // LOAD CACHE
            await cacheProvider.LoadAllAsync(message.DefinitionCache);

            // BOOT WORLD
            await bootstrapService.LoadAsync();

            logger.LogInformation("--------- World bootup successfully! Game started! ---------");
        }
        #endregion
    }
}