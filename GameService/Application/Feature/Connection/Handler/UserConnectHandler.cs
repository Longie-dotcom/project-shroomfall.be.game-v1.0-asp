using Application.Feature.Abstraction;
using Application.Feature.Connection.Command;
using Application.Interface.Realtime.Managers;

namespace Application.Feature.Connection.Handler
{
    public class UserConnectHandler : IHandler<UserConnectCommand>
    {
        #region Attributes
        private readonly IConnectionManager connectionManager;
        #endregion

        #region Properties
        #endregion

        public UserConnectHandler(
            IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        #region Methods
        public async Task Handle(
            UserConnectCommand command)
        {
            // Register the the connection (one connection per user only, replaced with old connection)
            connectionManager.Add(command.UserID, command.ConnectionID);
        }
        #endregion
    }
}