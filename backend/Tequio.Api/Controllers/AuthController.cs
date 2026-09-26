using Microsoft.AspNetCore.Mvc;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Controller responsible for managing user authentication, registration, and password recovery.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user (Buyer or Producer) in the platform.
        /// </summary>
        /// <param name="request">The user registration data.</param>
        /// <returns>A confirmation message requiring email verification.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            await _authService.RegisterUserAsync(request);
            return StatusCode(201, new
            {
                mensaje = "Usuario registrado exitosamente. Por favor, ingresa el código OTP enviado a tu correo."
            });
        }

        /// <summary>
        /// Authenticates a user and generates a JWT session token.
        /// </summary>
        /// <param name="request">The user login credentials.</param>
        /// <returns>A JWT token if authentication is successful.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            string token = await _authService.AuthenticateUserAsync(request);
            return Ok(new
            {
                mensaje = "Inicio de sesión exitoso.",
                token = token
            });
        }
    }
}
