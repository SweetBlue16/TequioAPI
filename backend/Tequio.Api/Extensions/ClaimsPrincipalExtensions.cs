using System.Security.Claims;

namespace Tequio.Api.Extensions
{
    /// <summary>
    /// Extension methods for ClaimsPrincipal to securely extract identity data from the JWT.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("El token proporcionado no contiene un identificador de usuario válido.");
            }
            return userId;
        }
    }
}
