using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;
using Tequio.Infrastructure.Data;
using Tequio.Infrastructure.Models;
using Xunit;

namespace Tequio.Tests.Auth;

public sealed class AdminAuthorizationHttpTest
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        public JwtSigningKey SigningKey { get; } = new(RandomNumberGenerator.GetBytes(32));

        public Mock<IAuthService> Auth { get; } = new();
        public IConfiguration Configuration { get; } = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TequioTests",
                ["Jwt:Audience"] = "TequioTests"
            }).Build();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddConfiguration(Configuration));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(SigningKey);
                services.RemoveAll<IAuthService>();
                services.AddSingleton(Auth.Object);
                services.RemoveAll<IUserService>();
                var users = new Mock<IUserService>();
                users.Setup(s => s.GetUserProfileAsync(7)).ReturnsAsync(
                    new UserProfileDto { Id = 7, Email = "master@example.invalid", IsVerified = true });
                services.AddSingleton(users.Object);
                services.RemoveAll<DataSeeder>();
                var seeder = new Mock<DataSeeder>(
                    new TequioDbContext(new DbContextOptionsBuilder<TequioDbContext>().Options), Configuration);
                seeder.Setup(s => s.SeedAdminAsync()).Returns(Task.CompletedTask);
                services.AddSingleton(seeder.Object);
            });
        }
        public HttpClient Client(string? role)
        {
            var client = CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", new JwtProvider(Configuration, SigningKey).GenerateToken("7", role));
            }
            return client;
        }
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("1", 403)]
    [InlineData("2", 403)]
    [InlineData("3", 403)]
    [InlineData("4", 200)]
    public async Task TestResendWithIdentityShouldEnforceMasterAdminOverHttp(string? role, int expected)
    {
        await using var factory = new Factory();
        using var client = factory.Client(role);
        var response = await client.PostAsJsonAsync("/api/v1/auth/resend-admin-authorization",
            new { email = "admin@example.invalid" });
        Assert.Equal(expected, (int)response.StatusCode);
        factory.Auth.Verify(s => s.ResendAdminAuthorizationAsync("admin@example.invalid", "master@example.invalid",
            It.IsAny<CancellationToken>()), expected == 200 ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task TestResendWithRateLimitShouldReturn429AndRetryAfter()
    {
        await using var factory = new Factory();
        factory.Auth.Setup(s => s.ResendAdminAuthorizationAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new VerificationCodeRateLimitException("Espera un minuto.", 60));
        using var client = factory.Client("4");
        var response = await client.PostAsJsonAsync("/api/v1/auth/resend-admin-authorization",
            new { email = "admin@example.invalid" });
        Assert.Equal(429, (int)response.StatusCode);
        Assert.Equal("60", Assert.Single(response.Headers.GetValues("Retry-After")));
    }

    [Fact]
    public async Task TestManagementWithLegacyAdminTokenShouldRejectBeforeAuthorization()
    {
        await using var factory = new Factory();
        var token = new JwtSecurityToken("TequioTests", "TequioTests", new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7"), new Claim(ClaimTypes.Role, "Admin")
        }, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
            factory.SigningKey.Key,
            SecurityAlgorithms.HmacSha256));
        using var client = factory.Client(null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        var response = await client.PatchAsync("/api/v1/delivery-points/1/deactive", null);
        Assert.Equal(401, (int)response.StatusCode);
    }
}
