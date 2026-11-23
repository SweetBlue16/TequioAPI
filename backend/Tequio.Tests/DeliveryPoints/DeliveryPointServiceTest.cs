using Moq;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Xunit;

namespace Tequio.Tests.DeliveryPoints;

public sealed class DeliveryPointServiceTest
{
    internal static SaveDeliveryPointDto Valid() => new()
    {
        Name = "TEST_Point",
        Street = "Enriquez",
        Number = "1",
        Neighborhood = "Centro",
        ZipCode = "91000",
        City = "Xalapa",
        BusinessHours = "09:00 - 18:00",
        Latitude = 19.53m,
        Longitude = -96.92m
    };

    public static TheoryData<decimal?, decimal?> InvalidCoordinates => new()
    {
        { null, 0m }, { 0m, null }, { null, null },
        { -90.000001m, 0m }, { 90.000001m, 0m }, { 0m, -180.000001m }, { 0m, 180.000001m }
    };

    [Theory]
    [MemberData(nameof(InvalidCoordinates))]
    public async Task TestSaveWithInvalidCoordinatesShouldRejectBeforePersistence(decimal? latitude, decimal? longitude)
    {
        foreach (int? id in new int?[] { null, 1 })
        {
            var repository = new Mock<IDeliveryPointRepository>(MockBehavior.Strict);
            var service = new DeliveryPointService(repository.Object);
            var dto = Valid();
            dto.Latitude = latitude;
            dto.Longitude = longitude;
            await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(id, dto));
            repository.VerifyNoOtherCalls();
        }
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public async Task TestSaveWithBoundaryCoordinatesShouldReachPersistence(int latitude, int longitude)
    {
        foreach (int? id in new int?[] { null, 1 })
        {
            var repository = new Mock<IDeliveryPointRepository>();
            var dto = Valid();
            dto.Latitude = latitude;
            dto.Longitude = longitude;
            repository.Setup(r => r.SaveAsync(id, dto)).ReturnsAsync(12);
            Assert.Equal(12, await new DeliveryPointService(repository.Object).SaveAsync(id, dto));
            repository.Verify(r => r.SaveAsync(id, dto), Times.Once);
        }
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task TestListWithInvalidPaginationShouldReject(int pageIndex, int pageSize)
    {
        var service = new DeliveryPointService(Mock.Of<IDeliveryPointRepository>());
        await Assert.ThrowsAsync<ArgumentException>(() => service.ListAsync(pageIndex, pageSize));
    }
}
