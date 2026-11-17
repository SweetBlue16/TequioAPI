using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Tequio.Infrastructure.Data;
using Tequio.Infrastructure.Models;
using Xunit;

namespace Tequio.Tests.DeliveryPoints;

/// <summary>Actual HTTP pipeline and JWT authorization; SQL is replaced only at the repository boundary.</summary>
public sealed class DeliveryPointHttpTest
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        public Mock<IDeliveryPointRepository> Repository { get; } = new();
        public IConfiguration TestConfiguration { get; } = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TEST_ONLY_SIGNING_KEY_WITH_MORE_THAN_32_CHARACTERS",
                ["Jwt:Issuer"] = "TequioTests", ["Jwt:Audience"] = "TequioTests"
            }).Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddConfiguration(TestConfiguration));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDeliveryPointRepository>();
                services.AddSingleton(Repository.Object);
                services.RemoveAll<DataSeeder>();
                var context = new TequioDbContext(new DbContextOptionsBuilder<TequioDbContext>().Options);
                var seeder = new Mock<DataSeeder>(context, TestConfiguration);
                seeder.Setup(s => s.SeedAdminAsync()).Returns(Task.CompletedTask);
                services.AddSingleton(seeder.Object);
            });
        }

        public HttpClient Client(string? role = "3")
        {
            var client = CreateClient();
            if (role is not null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", new JwtProvider(TestConfiguration).GenerateToken("123", role));
            }
            return client;
        }
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("1", 403)]
    [InlineData("2", 403)]
    [InlineData("4", 403)]
    public async Task TestManagementWithUnauthorizedIdentityShouldRejectEveryMutation(string? role, int expected)
    {
        await using var factory = new Factory();
        using var client = factory.Client(role);
        foreach (string method in new[] { "POST", "PUT", "PATCH", "ACTIVATE" })
        {
            using var response = method switch
            {
                "POST" => await client.PostAsJsonAsync("/api/v1/delivery-points", DeliveryPointServiceTest.Valid()),
                "PUT" => await client.PutAsJsonAsync("/api/v1/delivery-points/1", DeliveryPointServiceTest.Valid()),
                "PATCH" => await client.PatchAsync("/api/v1/delivery-points/1/deactive", null),
                _ => await client.PatchAsync("/api/v1/delivery-points/1/active", null)
            };
            Assert.Equal(expected, (int)response.StatusCode);
        }
        factory.Repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestCreateWithAdministratorShouldReturnCreatedAndLocation()
    {
        await using var factory = new Factory();
        factory.Repository.Setup(r => r.SaveAsync(null, It.IsAny<SaveDeliveryPointDto>())).ReturnsAsync(42);
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync("/api/v1/delivery-points", DeliveryPointServiceTest.Valid());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/delivery-points/42", response.Headers.Location?.ToString());
        Assert.Equal(42, (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"latitude\":\"NaN\",\"longitude\":0}")]
    [InlineData("{\"latitude\":1e999,\"longitude\":0}")]
    [InlineData("{\"latitude\":\"abc\",\"longitude\":0}")]
    [InlineData("{\"latitude\":91,\"longitude\":181}")]
    public async Task TestSaveWithInvalidJsonCoordinatesShouldReturnBadRequest(string body)
    {
        await using var factory = new Factory();
        using var client = factory.Client();
        foreach (string method in new[] { "POST", "PUT" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method),
                "/api/v1/delivery-points" + (method == "PUT" ? "/1" : ""));
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        factory.Repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestMutationsWithDatabaseConflictsShouldPreserveHttpErrorContract()
    {
        await using var factory = new Factory();
        factory.Repository.Setup(r => r.SaveAsync(null, It.IsAny<SaveDeliveryPointDto>()))
            .ThrowsAsync(new DeliveryPointConflictException("Ya existe un punto con ese nombre en el municipio."));
        factory.Repository.Setup(r => r.SaveAsync(1, It.IsAny<SaveDeliveryPointDto>()))
            .ThrowsAsync(new DeliveryPointConflictException("Ya existe un punto con ese nombre en el municipio."));
        factory.Repository.Setup(r => r.SetStatusAsync(1, false))
            .ThrowsAsync(new DeliveryPointConflictException("El punto tiene lotes vigentes."));
        using var client = factory.Client();
        using var create = await client.PostAsJsonAsync("/api/v1/delivery-points", DeliveryPointServiceTest.Valid());
        using var update = await client.PutAsJsonAsync("/api/v1/delivery-points/1", DeliveryPointServiceTest.Valid());
        using var deactivate = await client.PatchAsync("/api/v1/delivery-points/1/deactive", null);
        foreach (var response in new[] { create, update, deactivate })
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("error").GetString()));
        }
    }

    [Fact]
    public async Task TestQueriesWithAnonymousIdentityShouldRemainAccessible()
    {
        await using var factory = new Factory();
        factory.Repository.Setup(r => r.ListAsync(1, 10))
            .ReturnsAsync(new DeliveryPointPageDto([], 0, 1, 10));
        factory.Repository.Setup(r => r.GetAsync(99)).ThrowsAsync(new KeyNotFoundException("No se encontró el punto."));
        using var client = factory.Client(null);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/delivery-points")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/delivery-points/99")).StatusCode);
    }

    [Fact]
    public async Task TestUpdateWithMissingPointShouldReturnNotFound()
    {
        await using var factory = new Factory();
        factory.Repository.Setup(r => r.SaveAsync(99, It.IsAny<SaveDeliveryPointDto>()))
            .ThrowsAsync(new KeyNotFoundException("No se encontró el punto."));
        factory.Repository.Setup(r => r.SetStatusAsync(99, false))
            .ThrowsAsync(new KeyNotFoundException("No se encontró el punto."));
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync("/api/v1/delivery-points/99", DeliveryPointServiceTest.Valid())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PatchAsync("/api/v1/delivery-points/99/deactive", null)).StatusCode);
    }

    [Fact]
    public async Task TestSaveWithMissingMunicipalityShouldReturnBadRequest()
    {
        await using var factory = new Factory();
        factory.Repository.Setup(r => r.SaveAsync(null, It.IsAny<SaveDeliveryPointDto>()))
            .ThrowsAsync(new ArgumentException("El municipio no existe."));
        using var client = factory.Client();
        var dto = DeliveryPointServiceTest.Valid();
        dto.MunicipalityId = int.MaxValue;
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/v1/delivery-points", dto)).StatusCode);
    }

    [Fact]
    public async Task TestDeactivateWithUnlinkedPointShouldReturnSuccess()
    {
        await using var factory = new Factory();
        factory.Repository.Setup(r => r.SetStatusAsync(1, false)).Returns(Task.CompletedTask);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK,
            (await client.PatchAsync("/api/v1/delivery-points/1/deactive", null)).StatusCode);
        factory.Repository.Verify(r => r.SetStatusAsync(1, false), Times.Once);
    }
}
