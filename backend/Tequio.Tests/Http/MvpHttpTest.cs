using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Tequio.Api.Validators;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Tequio.Infrastructure.Data;
using Tequio.Infrastructure.Models;
using Xunit;

namespace Tequio.Tests.Http;

/// <summary>Exercises the API contracts through the real MVC, authorization and domain pipeline.</summary>
public sealed class MvpHttpTest
{
    private const int UserId = 7;
    private const string Email = "account@example.invalid";
    private const string StorageUrl = "https://images.example.invalid/menu.jpg";
    private const string ProductRoute = "/api/v1/products";
    private static readonly byte[] ImageBytes = [0xff, 0xd8, 0xff, 0xd9];

    [Fact]
    public async Task TestCatalogWithSecondPageShouldTranslateIndexAndReturnProducts()
    {
        await using var factory = new Factory();
        var page = new ProductPageDto
        {
            ItemCount = 1,
            PageIndex = 1,
            PageSize = 10,
            Products = [new ProductDto { Id = 12, Name = "Café", IsActive = true }],
        };
        factory.Products.Setup(repository => repository.GetProductsByCategoryAsync(3, 1, 10, default))
            .ReturnsAsync(page);
        factory.Products.Setup(repository => repository.GetProductsFromProducerAsync(UserId, 1, 10, default))
            .ReturnsAsync(page);

        using var client = factory.Client();
        foreach (var path in new[] { "category/3", $"producers/{UserId}" })
        {
            var response = await client.GetFromJsonAsync<ProductPageDto>(
                $"{ProductRoute}/{path}?pageIndex=2&pageSize=10");
            Assert.NotNull(response);
            Assert.Equal(1, response.PageIndex);
            Assert.Equal(12, Assert.Single(response.Products).Id);
        }

        factory.Products.VerifyAll();
    }

    [Theory]
    [InlineData("text", "1", "10")]
    [InlineData("-1", "1", "10")]
    [InlineData("1", "0", "9")]
    [InlineData("1", "2147483647", "101")]
    public async Task TestCatalogWithInvalidQueryShouldRejectBeforeDatabase(string category, string page, string size)
    {
        await using var factory = new Factory();
        using var client = factory.Client();
        using var response = await client.GetAsync(
            $"{ProductRoute}/category/{category}?pageIndex={page}&pageSize={size}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        factory.Products.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestCreateProductWithProducerShouldPersistTrimmedFieldsAndReturnIdentifier()
    {
        await using var factory = new Factory();
        factory.Products.Setup(repository => repository.CreateBaseProductAsync(
            UserId,
            It.Is<CreateBaseProductDto>(dto => dto.Name == "Café" && dto.CategoryId == 1),
            default)).ReturnsAsync(12);

        using var client = factory.Client("2");
        using var response = await client.PostAsJsonAsync(ProductRoute, new
        {
            categoryId = 1,
            name = " Café ",
            shortDescription = " Producto local ",
            measurementUnit = " kg ",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(12, (await ReadBody(response)).GetProperty("productId").GetInt32());
        factory.Products.VerifyAll();
    }

    [Fact]
    public async Task TestDeactivateProductWithOwnershipAndConflictShouldReturnContractStatuses()
    {
        await using var factory = new Factory();
        factory.Products.Setup(repository => repository.DeactivateBaseProductAsync(1, UserId, default))
            .Returns(Task.CompletedTask);
        factory.Products.Setup(repository => repository.DeactivateBaseProductAsync(2, UserId, default))
            .ThrowsAsync(new ProductConflictException("El producto ya no se puede dar de baja."));
        factory.Products.Setup(repository => repository.DeactivateBaseProductAsync(3, UserId, default))
            .ThrowsAsync(new ProductOwnershipException("El producto no está disponible."));

        using var client = factory.Client("2");
        using var changed = await client.PatchAsync($"{ProductRoute}/1/deactive", null);
        using var conflict = await client.PatchAsync($"{ProductRoute}/2/deactive", null);
        using var forbidden = await client.PatchAsync($"{ProductRoute}/3/deactive", null);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        factory.Products.VerifyAll();
    }

    [Fact]
    public async Task TestProfileWithBearerIdentityShouldUpdateOnlyTheCurrentUser()
    {
        await using var factory = new Factory();
        const string route = "/api/v1/users/me";
        factory.Users.Setup(repository => repository.GetUserProfileAsync(UserId))
            .ReturnsAsync(new UserProfileDto { Id = UserId, Email = Email, IsVerified = true });
        factory.Users.Setup(repository => repository.UpdateUserProfileAsync(
            UserId, It.Is<UpdateUserProfileDto>(profile => profile.Locality == "Xalapa")))
            .Returns(Task.CompletedTask);
        factory.Users.Setup(repository => repository.UpdateProfilePictureAsync(UserId, StorageUrl))
            .Returns(Task.CompletedTask);
        factory.Users.Setup(repository => repository.PromoteToProducerAsync(
            UserId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        using var client = factory.Client("1");
        var profile = await client.GetFromJsonAsync<UserProfileDto>(route);
        Assert.Equal(UserId, profile?.Id);
        using var update = await client.PutAsJsonAsync(route, new
        {
            userId = 99,
            firstName = "Ana",
            paternalLastName = "Prueba",
            birthDate = "1990-04-12",
            locality = "Xalapa",
        });
        using var picture = await client.PutAsJsonAsync(route + "/profile-picture", new { pictureUrl = StorageUrl });
        using var promote = await client.PatchAsync(route + "/role/producer", null);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, picture.StatusCode);
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);
        factory.Users.VerifyAll();
    }

    [Fact]
    public async Task TestUploadWithImageShouldReturnStorageUrlAndRejectEmptyContent()
    {
        await using var factory = new Factory();
        factory.Storage.Setup(storage => storage.UploadFileAsync(It.IsAny<Stream>(), "menu.jpg"))
            .ReturnsAsync(StorageUrl);
        using var client = factory.Client("1");
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(ImageBytes), "file", "menu.jpg");
        using var response = await client.PostAsync("/api/v1/files/image", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StorageUrl, (await ReadBody(response)).GetProperty("url").GetString());

        using var emptyForm = new MultipartFormDataContent();
        emptyForm.Add(new ByteArrayContent([]), "file", "menu.jpg");
        using var rejected = await client.PostAsync("/api/v1/files/image", emptyForm);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        factory.Storage.Verify(storage => storage.UploadFileAsync(It.IsAny<Stream>(), "menu.jpg"), Times.Once);
    }

    [Fact]
    public async Task TestAuthEndpointsWithValidRequestsShouldDelegateAndPreservePublicResponse()
    {
        await using var factory = new Factory();
        factory.Auth.Setup(service => service.AuthenticateUserAsync(
            It.Is<LoginRequestDto>(request => request.Email == Email), It.IsAny<CancellationToken>()))
            .ReturnsAsync("fixture-session");
        factory.Auth.Setup(service => service.VerifyAccountAsync(
            It.Is<VerifyAccountRequestDto>(request => request.Email == Email), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        factory.Auth.Setup(service => service.ResendVerificationCodeAsync(Email, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        factory.Auth.Setup(service => service.SendPasswordRecoveryCodeAsync(Email, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        factory.Auth.Setup(service => service.ResetPasswordAsync(
            It.Is<ResetPasswordRequestDto>(request => request.Email == Email), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = factory.Client();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = Email,
            password = "FixturePassword1!",
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("fixture-session", (await ReadBody(login)).GetProperty("token").GetString());

        var operations = new Dictionary<string, object>
        {
            ["verify"] = new { email = Email, verificationCode = "123456" },
            ["resend-verification-code"] = new { email = Email },
            ["recover"] = new { email = Email },
            ["reset-password"] = new { email = Email, code = "123456", newPassword = "NewFixturePassword1!" },
        };
        foreach (var (route, payload) in operations)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/" + route, payload);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True((await ReadBody(response)).TryGetProperty("mensaje", out _));
        }

        factory.Auth.VerifyAll();
    }

    [Fact]
    public async Task TestAdminAuthorizationWithMasterAdminShouldPreserveScopeAndTarget()
    {
        await using var factory = new Factory();
        factory.Users.Setup(repository => repository.GetUserProfileAsync(UserId))
            .ReturnsAsync(new UserProfileDto { Id = UserId, Email = Email });
        factory.Auth.Setup(service => service.RegisterAdminAsync(
            It.IsAny<RegisterRequestDto>(), Email, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        factory.Auth.Setup(service => service.AuthorizeAdminAsync(
            It.Is<AuthorizeAdminRequestDto>(request => request.NewAdminEmail == "admin@example.invalid"),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        using var client = factory.Client("4");
        using var register = await client.PostAsJsonAsync("/api/v1/auth/register-admin", new
        {
            email = "admin@example.invalid",
            password = "FixturePassword1!",
            roleId = 1,
            firstName = "Ana",
            paternalLastName = "Prueba",
            birthDate = "1990-04-12",
            phoneNumber = "2281234567",
        });
        using var authorize = await client.PostAsJsonAsync("/api/v1/auth/authorize-admin", new
        {
            newAdminEmail = "admin@example.invalid",
            verificationCode = "123456",
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authorize.StatusCode);
        factory.Auth.VerifyAll();
    }

    [Fact]
    public void TestSigningKeyWithInsufficientMaterialShouldFailBeforeSigning()
    {
        Assert.Throws<ArgumentException>(() => new JwtSigningKey(new byte[31]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void TestIdentifierWithNonPositiveValueShouldReject(int id)
    {
        Assert.Throws<ArgumentException>(() => NumberValidator.ValidateGreaterOrEqualToOne(id, "id"));
        NumberValidator.ValidateGreaterOrEqualToOne(1, "id");
    }

    private static async Task<JsonElement> ReadBody(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly JwtSigningKey _signingKey = new(RandomNumberGenerator.GetBytes(32));
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TequioTests",
                ["Jwt:Audience"] = "TequioTests",
            }).Build();

        public Mock<IProductRepository> Products { get; } = new(MockBehavior.Strict);
        public Mock<IUserRepository> Users { get; } = new(MockBehavior.Strict);
        public Mock<IAuthService> Auth { get; } = new(MockBehavior.Strict);
        public Mock<IStorageService> Storage { get; } = new(MockBehavior.Strict);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddConfiguration(_configuration));
            builder.ConfigureLogging(logging =>
                logging.AddFilter("Tequio", LogLevel.Information));
            builder.ConfigureServices(services =>
            {
                var database = new TequioDbContext(new DbContextOptionsBuilder<TequioDbContext>().Options);
                var seeder = new Mock<DataSeeder>(database, _configuration);
                seeder.Setup(service => service.SeedAdminAsync()).Returns(Task.CompletedTask);
                services.AddSingleton(seeder.Object);
                services.AddSingleton(_signingKey);
                services.AddSingleton(Products.Object);
                services.AddSingleton(Users.Object);
                services.AddSingleton(Auth.Object);
                services.AddSingleton(Storage.Object);
            });
        }

        public HttpClient Client(string? role = null)
        {
            var client = CreateClient();
            if (role is not null)
            {
                var token = new JwtProvider(_configuration, _signingKey).GenerateToken("7", role);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}
