using Microsoft.AspNetCore.Mvc;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers;

/// <summary>
/// Manages user authentication, registration, verification, and password-recovery requests.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">Authentication service.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user and sends an account-verification code.
    /// </summary>
    /// <param name="request">User registration data.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A confirmation message requiring email verification.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.RegisterUserAsync(request, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                mensaje = "Usuario registrado exitosamente. Revisa tu correo e ingresa el código de verificación."
            });
    }

    /// <summary>
    /// Authenticates a user and generates a JWT session token.
    /// </summary>
    /// <param name="request">User login credentials.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A JWT token if authentication is successful.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        string token = await _authService.AuthenticateUserAsync(request, cancellationToken);

        return Ok(new
        {
            mensaje = "Inicio de sesión exitoso.",
            token
        });
    }

    /// <summary>
    /// Verifies a user account with a six-digit code.
    /// </summary>
    /// <param name="request">Email and verification code.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(
        [FromBody] VerifyAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.VerifyAccountAsync(request, cancellationToken);
        return Ok(new { mensaje = "Tu cuenta fue verificada exitosamente." });
    }

    /// <summary>
    /// Sends a new account-verification code when appropriate.
    /// </summary>
    /// <param name="request">Email address associated with the account.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A generic confirmation message that prevents account enumeration.</returns>
    [HttpPost("resend-verification-code")]
    public async Task<IActionResult> ResendVerificationCode(
        [FromBody] EmailAddressRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.ResendVerificationCodeAsync(request.Email, cancellationToken);

        return Ok(new
        {
            mensaje = "Si la cuenta existe y está pendiente de verificación, recibirás un nuevo código por correo."
        });
    }

    /// <summary>
    /// Starts password recovery by sending a security code when the account exists.
    /// </summary>
    /// <param name="request">Email address associated with the account.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A generic confirmation message that prevents account enumeration.</returns>
    [HttpPost("recover")]
    public async Task<IActionResult> Recover(
        [FromBody] EmailAddressRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.SendPasswordRecoveryCodeAsync(request.Email, cancellationToken);

        return Ok(new
        {
            mensaje = "Si existe una cuenta asociada, recibirás un código para recuperar tu cuenta."
        });
    }
}
