using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Tequio.Tests.DeliveryPoints;
using Xunit;
using Xunit.Abstractions;

namespace Tequio.Tests.Batches;

/// <summary>Verifies publication and stock accounting on the isolated SQL Server database.</summary>
public sealed class BatchStockSqlTest(ITestOutputHelper output)
{
    private const int Stock = 10;
    private const int ConcurrentBuyers = 50;
    private const int InsufficientStock = 50208;

    [DeliverySqlFact]
    public async Task TestPublicationWithInvalidPackagesAndInactivePointShouldRejectBoth()
    {
        var fixture = await CreateCatalogAsync();

        var empty = await Assert.ThrowsAsync<SqlException>(() =>
            PublishAsync(fixture, "[]", DateTime.UtcNow.AddDays(1)));
        Assert.Equal(50201, empty.Number);

        var expired = await Assert.ThrowsAsync<SqlException>(() =>
            PublishAsync(fixture, CreatePackages(), DateTime.UtcNow.AddDays(-1)));
        Assert.Equal(50201, expired.Number);

        await ScalarAsync(
            "UPDATE dbo.DeliveryPoint SET IsActive = 0 WHERE Id = @id",
            Id(fixture.PointId));

        var inactive = await Assert.ThrowsAsync<SqlException>(() =>
            PublishAsync(fixture, CreatePackages(), DateTime.UtcNow.AddDays(1)));
        Assert.Equal(50203, inactive.Number);
        Assert.Equal(0, await ScalarAsync(
            "SELECT COUNT(*) FROM dbo.Batch WHERE BaseProductId = @id",
            Id(fixture.ProductId)));
    }

    [DeliverySqlFact]
    public async Task TestReservationWithConcurrentBuyersShouldPreserveStockAndRejectDuplicates()
    {
        var fixture = await CreateCatalogAsync();
        int batchId = await PublishAsync(fixture, CreatePackages(), DateTime.UtcNow.AddDays(1));
        Assert.Equal(1, await ScalarAsync(
            "SELECT COUNT(*) FROM dbo.Batch WHERE Id = @id AND Status = N'Active'",
            Id(batchId)));
        int packageId = await ScalarAsync("SELECT Id FROM dbo.Package WHERE BatchId = @id", Id(batchId));
        string packages = JsonSerializer.Serialize(new[] { new { packageId, quantity = 1 } });

        var buyers = new List<int>();
        for (int index = 0; index < ConcurrentBuyers; index++)
        {
            buyers.Add(await CreateUserAsync(1));
        }

        var allRequests = buyers.Select(buyerId => AttemptAsync(buyerId, batchId, packages));
        var results = await Task.WhenAll(allRequests);
        var successful = results.Where(result => result.OrderId.HasValue).ToArray();

        Assert.Equal(Stock, successful.Length);
        Assert.Equal(ConcurrentBuyers - Stock, results.Count(result => result.Error == InsufficientStock));
        Assert.Equal(0, await ScalarAsync("SELECT AvailableQuantity FROM dbo.Package WHERE Id = @id", Id(packageId)));
        Assert.Equal(Stock, await ScalarAsync(
            "SELECT COUNT(*) FROM dbo.[Order] WHERE BatchId = @id AND PaymentStatus = N'Pending' AND TotalPaid = 0",
            Id(batchId)));

        var winner = successful[0];
        Assert.Equal(winner.OrderId, await ReserveAsync(winner.BuyerId, batchId, winner.RequestId, packages));

        string differentQuantity = JsonSerializer.Serialize(new[] { new { packageId, quantity = 2 } });
        var changed = await Assert.ThrowsAsync<SqlException>(() =>
            ReserveAsync(winner.BuyerId, batchId, winner.RequestId, differentQuantity));
        Assert.Equal(50206, changed.Number);
        Assert.Equal(Stock, await ScalarAsync(
            """
            SELECT SUM(detail.Quantity)
            FROM dbo.OrderDetail AS detail
            JOIN dbo.[Order] AS reservation ON reservation.Id = detail.OrderId
            WHERE reservation.BatchId = @id
            """,
            Id(batchId)));

        double[] latencies = results.Select(result => result.DurationMs).Order().ToArray();
        double p95 = latencies[(int)Math.Ceiling(0.95 * latencies.Length) - 1];
        output.WriteLine(
            $"RN-03 | SQL only | buyers={ConcurrentBuyers} | stock={Stock} | " +
            $"succeeded={successful.Length} | p95-ms={p95:F2}");
    }

    private static async Task<ReservationAttempt> AttemptAsync(int buyerId, int batchId, string packages)
    {
        Guid requestId = Guid.NewGuid();
        var watch = Stopwatch.StartNew();
        try
        {
            int orderId = await ReserveAsync(buyerId, batchId, requestId, packages);
            return new ReservationAttempt(buyerId, requestId, orderId, null, watch.Elapsed.TotalMilliseconds);
        }
        catch (SqlException exception) when (exception.Number == InsufficientStock)
        {
            return new ReservationAttempt(buyerId, requestId, null, exception.Number, watch.Elapsed.TotalMilliseconds);
        }
    }

    private static Task<int> ReserveAsync(int buyerId, int batchId, Guid requestId, string packages)
    {
        return ScalarAsync(
            "dbo.USP_ReserveBatchStock",
            new SqlParameter("@buyerId", buyerId),
            new SqlParameter("@batchId", batchId),
            new SqlParameter("@requestId", requestId),
            new SqlParameter("@packages", SqlDbType.NVarChar, -1) { Value = packages });
    }

    private static Task<int> PublishAsync(CatalogFixture fixture, string packages, DateTime deadline)
    {
        return ScalarAsync(
            "dbo.USP_PublishBatch",
            new SqlParameter("@producerId", fixture.ProducerId),
            new SqlParameter("@productId", fixture.ProductId),
            new SqlParameter("@deliveryPointId", fixture.PointId),
            new SqlParameter("@name", "TEST_" + Guid.NewGuid().ToString("N")),
            new SqlParameter("@description", "Lote de prueba"),
            new SqlParameter("@minimumGoal", 5),
            new SqlParameter("@deadline", deadline),
            new SqlParameter("@packages", SqlDbType.NVarChar, -1) { Value = packages });
    }

    private static string CreatePackages()
    {
        return JsonSerializer.Serialize(new[]
        {
            new { name = "Paquete de café", content = "Funda de 250 g", price = 40, quantity = Stock }
        });
    }

    private static async Task<CatalogFixture> CreateCatalogAsync()
    {
        int producer = await CreateUserAsync(2);
        string name = "TEST_" + Guid.NewGuid().ToString("N");
        int product = await ScalarAsync(
            """
            INSERT INTO dbo.BaseProduct (ProducerId, CategoryId, Name, ShortDescription, MeasurementUnit)
            OUTPUT INSERTED.Id
            VALUES (@producerId, 1, @name, N'Prueba de lotes', N'piezas');
            """,
            new SqlParameter("@producerId", producer),
            new SqlParameter("@name", name));
        int point = await ScalarAsync(
            "dbo.USP_CreateDeliveryPoint",
            new SqlParameter("@name", name),
            new SqlParameter("@street", "Enríquez"),
            new SqlParameter("@number", "1"),
            new SqlParameter("@neighborhood", "Centro"),
            new SqlParameter("@zipCode", "91000"),
            new SqlParameter("@city", "Xalapa"),
            new SqlParameter("@businessHours", "09:00 - 18:00"),
            new SqlParameter("@latitude", 19.53m),
            new SqlParameter("@longitude", -96.92m));
        return new CatalogFixture(producer, product, point);
    }

    private static Task<int> CreateUserAsync(int roleId)
    {
        return ScalarAsync(
            """
            INSERT INTO dbo.[User] (RoleId, Email, PasswordHash, FirstName, PaternalLastName, BirthDate, IsVerified)
            OUTPUT INSERTED.Id
            VALUES (@roleId, @email, N'unused-test-fixture', N'TEST_Batch', N'Test', '20000101', 1);
            """,
            new SqlParameter("@roleId", roleId),
            new SqlParameter("@email", $"batch-{Guid.NewGuid():N}@example.invalid"));
    }

    private static SqlParameter Id(int value) => new("@id", value);

    private static async Task<int> ScalarAsync(string statement, params SqlParameter[] parameters)
    {
        var connectionString = Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = statement;
        command.CommandType = statement.StartsWith("dbo.USP_", StringComparison.Ordinal)
            ? CommandType.StoredProcedure
            : CommandType.Text;
        command.Parameters.AddRange(parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private sealed record CatalogFixture(int ProducerId, int ProductId, int PointId);

    private sealed record ReservationAttempt(
        int BuyerId, Guid RequestId, int? OrderId, int? Error, double DurationMs);
}
