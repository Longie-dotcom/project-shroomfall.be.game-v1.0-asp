using Application.Interface.Cache;
using Contract.DTO.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Consumer
{
    public class DefinitionCacheConsumer : IConsumer<DefinitionCacheDTO>
    {
        #region Attributes
        private readonly ILogger<DefinitionCacheConsumer> logger;
        private readonly ICacheProvider cacheProvider;
        #endregion

        #region Properties
        #endregion

        public DefinitionCacheConsumer(
            ILogger<DefinitionCacheConsumer> logger,
            ICacheProvider cacheProvider)
        {
            this.logger = logger;
            this.cacheProvider = cacheProvider;
        }

        #region Methods
        public async Task Consume(
            ConsumeContext<DefinitionCacheDTO> context)
        {
            var message = context.Message;

            // LOAD CACHE
            await cacheProvider.LoadAllAsync(message);

            logger.LogInformation("--------- Definition cache reloaded successfully! ---------");
        }
        #endregion
    }
}