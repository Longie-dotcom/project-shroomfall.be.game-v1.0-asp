using Application.Feature.Abstraction;
using Application.Feature.Connection.Command;
using Application.Interface.Realtime.Managers;

namespace Application.Feature.Connection.Handler
{
    public class UserDisconnectHandler : IHandler<UserDisconnectCommand>
    {
        #region Attributes
        private readonly IConnectionManager connectionManager;
        #endregion

        #region Properties
        #endregion

        public UserDisconnectHandler(
            IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        #region Methods
        public async Task Handle(
            UserDisconnectCommand command)
        {
            // Remove the the connection
            connectionManager.Remove(command.UserID, command.ConnectionID);
        }
        #endregion
    }
}