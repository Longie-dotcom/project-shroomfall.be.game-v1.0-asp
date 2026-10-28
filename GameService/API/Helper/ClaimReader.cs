using Domain.DomainException;
using ResponseCode;
using System.Security.Claims;

namespace API.Helper
{
    public static class ClaimReader
    {
        #region Methods

        public static (string UserId, string? GoogleId, string? Role) GetIdentity(
            ClaimsPrincipal user)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new Unauthorized(
                    APICode.ClaimReaderCode.ClaimMissingUserId,
                    "User has no user ID in token");

            var googleId = user.FindFirst("googleId")?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            return (userId, googleId, role);
        }

        public static string? GetRole(
            ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value;
        }

        public static bool IsInRole(
            ClaimsPrincipal user, 
            string role)
        {
            return user.IsInRole(role);
        }

        public static bool IsAuthenticated(
            ClaimsPrincipal user)
        {
            return user?.Identity?.IsAuthenticated ?? false;
        }

        #endregion
    }
}