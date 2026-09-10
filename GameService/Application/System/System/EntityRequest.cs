using Application.Service.EntityService;
using Application.Service.MetaService;
using Application.Service.WorldService;
using Application.System.Queue;

namespace Application.System.System
{
    public class EntityRequest
    {
        #region Attributes
        private readonly EffectService effectService;
        private readonly AIService aiService;
        private readonly TransformService transformService;
        private readonly ProjectileService projectileService;
        private readonly LifetimeService lifetimeService;
        private readonly ItemService itemService;
        private readonly CharacteristicService characteristicService;
        private readonly ResidencyService residencyService;
        #endregion

        #region Properties
        #endregion

        public EntityRequest(
            EffectService effectService,
            AIService aiService,
            TransformService transformService,
            ProjectileService projectileService,
            LifetimeService lifetimeService,
            ItemService itemService,
            CharacteristicService characteristicService,
            ResidencyService residencyService)
        {
            this.effectService = effectService;
            this.aiService = aiService;
            this.transformService = transformService;
            this.projectileService = projectileService;
            this.lifetimeService = lifetimeService;
            this.itemService = itemService;
            this.characteristicService = characteristicService;
            this.residencyService = residencyService;
        }

        #region Methods
        public async Task Tick(
            float dt,
            CommandBuffer commandBuffer)
        {
            aiService.Tick(dt, commandBuffer);
            effectService.Tick(dt, commandBuffer);
            transformService.Tick(dt, commandBuffer);
            projectileService.Tick(dt, commandBuffer);
            lifetimeService.Tick(dt, commandBuffer);
            itemService.Tick(dt, commandBuffer);
            characteristicService.Tick(dt, commandBuffer);
            
            await residencyService.Tick(dt);
        }
        #endregion
    }
}