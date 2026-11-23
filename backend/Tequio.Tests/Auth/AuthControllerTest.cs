using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Tequio.Api.Controllers;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;
using Xunit;

namespace Tequio.Tests.Auth;

/// <summary>
/// Tests for authentication HTTP responses.
/// </summary>
public sealed class AuthControllerTest
{
    /// <summary>
    /// Verifies the registration response never exposes the generated verification code.
    /// </summary>
    [Fact]
    public async Task TestRegisterWithSuccessfulFlowShouldNotReturnVerificationCode()
    {
        var authService = new Mock<IAuthService>();
        var logger = new Mock<ILogger<AuthController>>();
        var request = new RegisterRequestDto
        {
            Email = "buyer@example.test",
            Password = "SecurePassword123!",
            FirstName = "Ana",
            PaternalLastName = "López",
            BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RoleId = 1
        };
        authService
            .Setup(service => service.RegisterUserAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        var controller = new AuthController(authService.Object, logger.Object);

        IActionResult actionResult = await controller.Register(request, CancellationToken.None);

        ObjectResult result = Assert.IsType<ObjectResult>(actionResult);
        string responseBody = JsonSerializer.Serialize(result.Value);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.DoesNotContain("004281", responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("verificationCode", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Uses the authenticated profile because existing JWTs do not carry an email claim.</summary>
    [Fact]
    public async Task TestRegisterAdminWithTokenWithoutEmailShouldUseAuthenticatedProfileEmail()
    {
        var authService = new Mock<IAuthService>();
        var userService = new Mock<IUserService>();
        userService.Setup(service => service.GetUserProfileAsync(7))
            .ReturnsAsync(new UserProfileDto { Id = 7, Email = "master@example.invalid" });
        var controller = new AuthController(authService.Object, Mock.Of<ILogger<AuthController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                        new[]
                        {
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "7"),
                            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "MasterAdmin")
                        }, "Test"))
                }
            }
        };
        var request = new RegisterRequestDto { Email = "admin@example.invalid" };

        var response = await controller.RegisterAdmin(request, userService.Object, CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(response).StatusCode);
        authService.Verify(service => service.RegisterAdminAsync(
            request, "master@example.invalid", CancellationToken.None), Times.Once);
        userService.Verify(service => service.GetUserProfileAsync(7), Times.Once);
    }

}
