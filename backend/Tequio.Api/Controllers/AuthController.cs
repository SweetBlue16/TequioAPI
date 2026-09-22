using Microsoft.AspNetCore.Mvc;
using Tequio.Infrastructure.Models;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Controller responsible for managing user authentication, registration, and password recovery.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly TequioDbContext _dbContext;

        public AuthController(TequioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Registers a new user (Buyer or Producer) in the platform.
        /// </summary>
        /// <returns>A confirmation message requiring email verification.</returns>
        [HttpPost("register")]
        public IActionResult Register()
        {
            // TODO: Implement domain logic for user creation, role assignment, and validation
            return StatusCode(201, new
            {
                mensaje = "Usuario registrado exitosamente. Por favor, ingresa el código OTP enviado a tu correo."
            });
        }
    }
}
