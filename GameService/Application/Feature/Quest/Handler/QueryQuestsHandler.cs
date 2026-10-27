using Application.Feature.Abstraction;
using Application.Feature.Quest.Command;
using Application.Service.MetaService;
using Contract.DTO.Abstraction;
using Contract.DTO.Runtime.MetaDomain;

namespace Application.Feature.Quest.Handler
{
    public class QueryQuestsHandler : IHandler<QueryQuestsCommand, PagedResponseDTO<QuestInstanceDTO>>
    {
        #region Attributes
        private readonly QuestService questService;
        #endregion

        #region Properties
        #endregion

        public QueryQuestsHandler(
            QuestService questService)
        {
            this.questService = questService;
        }

        #region Methods
        public async Task<PagedResponseDTO<QuestInstanceDTO>> Handle(
            QueryQuestsCommand command)
        {
            var dto = command.DTO;
            var (items, count) = questService.QueryQuests(dto);
            return new PagedResponseDTO<QuestInstanceDTO>
            {
                Items = items,
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                TotalCount = count
            };
        }
        #endregion
    }
}