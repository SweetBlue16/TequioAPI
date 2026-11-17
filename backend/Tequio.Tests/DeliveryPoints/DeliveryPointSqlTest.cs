using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Tequio.Tests.DeliveryPoints;

/// <summary>Skips honestly when an isolated installed SQL database is not supplied.</summary>
public sealed class DeliverySqlFactAttribute : FactAttribute
{
    public DeliverySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL")))
        {
            Skip = "Configura TEQUIO_TEST_SQL con una base TEQUIO aislada e instala 01-17 antes de ejecutar.";
        }
    }
}

/// <summary>Exercises real procedures, constraints and triggers; every fixture is rolled back.</summary>
public sealed class DeliveryPointSqlTest
{
    private static async Task WithFixture(Func<SqlConnection, SqlTransaction, string, Task> test)
    {
        await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL"));
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            await test(connection, transaction, "TEST_" + Guid.NewGuid().ToString("N"));
        }
        finally
        {
            // RN-07's XACT_ABORT can already have rolled back the transaction.
            if (transaction.Connection is not null)
            {
                await transaction.RollbackAsync();
            }
        }
    }

    private static async Task<object?> Execute(SqlConnection connection, SqlTransaction transaction, string sql)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        return await command.ExecuteScalarAsync();
    }

    private static async Task<int> Create(SqlConnection connection, SqlTransaction transaction, string name,
        string extra = "", int? id = null)
    {
        await using var command = new SqlCommand(id.HasValue ? "USP_UpdateDeliveryPoint" : "USP_CreateDeliveryPoint",
            connection, transaction) { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@street", "Enriquez");
        command.Parameters.AddWithValue("@number", "1");
        command.Parameters.AddWithValue("@neighborhood", "Centro");
        command.Parameters.AddWithValue("@zipCode", "91000");
        command.Parameters.AddWithValue("@city", "Xalapa");
        command.Parameters.AddWithValue("@businessHours", "09:00 - 18:00");
        command.Parameters.AddWithValue("@latitude", 0m);
        command.Parameters.AddWithValue("@longitude", 0m);
        if (id.HasValue)
        {
            command.Parameters.AddWithValue("@id", id.Value);
        }
        if (extra == "missingMunicipality")
        {
            command.Parameters.AddWithValue("@municipalityId", int.MaxValue);
        }
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    [DeliverySqlFact]
    public Task TestCreateWithDuplicateNameShouldRejectNormalizedDuplicate() => WithFixture(async (c, t, run) =>
    {
        await Create(c, t, run + "_Café");
        var error = await Assert.ThrowsAsync<SqlException>(() => Create(c, t, "  " + run.ToLowerInvariant() + "_cafe  "));
        Assert.Contains(error.Number, new[] { 2601, 2627 });
    });

    [DeliverySqlFact]
    public Task TestUpdateWithDuplicateNameShouldPreserveOriginal() => WithFixture(async (c, t, run) =>
    {
        await Create(c, t, run);
        int second = await Create(c, t, run + "_second");
        var error = await Assert.ThrowsAsync<SqlException>(() => Create(c, t, run, id: second));
        Assert.Contains(error.Number, new[] { 2601, 2627 });
        // The update statement is atomic; XACT_ABORT may abort the enclosing fixture transaction.
    });

    [DeliverySqlFact]
    public Task TestCreateWithMissingMunicipalityShouldReject() => WithFixture(async (c, t, run) =>
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => Create(c, t, run, "missingMunicipality"));
        Assert.Equal(50105, error.Number);
    });

    [DeliverySqlFact]
    public Task TestDeactivateWithNoBatchShouldRetainInactiveRecord() => WithFixture(async (c, t, run) =>
    {
        int id = await Create(c, t, run);
        await Execute(c, t, $"EXEC dbo.USP_SetDeliveryPointStatus @id={id}, @isActive=0;");
        Assert.False(Convert.ToBoolean(await Execute(c, t, $"SELECT IsActive FROM dbo.DeliveryPoint WHERE Id={id}")));
    });

    [DeliverySqlFact]
    public Task TestDeactivateWithMissingPointShouldReject() => WithFixture(async (c, t, run) =>
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => Execute(c, t,
            "EXEC dbo.USP_SetDeliveryPointStatus @id=2147483647, @isActive=0;"));
        Assert.Equal(50104, error.Number);
    });

    [DeliverySqlFact]
    public Task TestBatchStatesWithCurrentOrTerminalValuesShouldFollowRn07() => WithFixture(async (c, t, run) =>
    {
        foreach (string status in new[] { "Draft", "Active", "Confirmed", "Vigente", "Unknown" })
        {
            Assert.True(Convert.ToBoolean(await Execute(c, t, $"SELECT dbo.UF_IsCurrentBatch(N'{status}')")));
        }
        foreach (string status in new[] { "Cancelled", "Completed", "Cancelado", "Finalizado", "Entregado" })
        {
            Assert.False(Convert.ToBoolean(await Execute(c, t, $"SELECT dbo.UF_IsCurrentBatch(N'{status}')")));
        }
    });

    [DeliverySqlFact]
    public Task TestCreateWithOutOfRangeCoordinatesShouldRejectInSql() => WithFixture(async (c, t, run) =>
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => Execute(c, t,
            $"EXEC dbo.USP_CreateDeliveryPoint @name=N'{run}', @street=N'A', @number=N'1', " +
            "@neighborhood=N'Centro', @zipCode=N'91000', @city=N'Xalapa', @businessHours=N'09:00 - 18:00', " +
            "@latitude=91, @longitude=0;"));
        Assert.Equal(50101, error.Number);
    });
    [DeliverySqlFact]
    public async Task TestDeactivateWithCurrentBatchesShouldPreserveRecordAndAssociations()
    {
        await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL"));
        await connection.OpenAsync();
        string run = "TEST_" + Guid.NewGuid().ToString("N");
        int pointId;
        int batchId;
        int productId;
        int producerId;
        // Commit only this test's fixtures so RN-07's rollback cannot erase the evidence.
        await using (var transaction = (SqlTransaction)await connection.BeginTransactionAsync())
        {
            pointId = await Create(connection, transaction, run);
            producerId = Convert.ToInt32(await Execute(connection, transaction,
                $"INSERT dbo.[User] (RoleId,Email,PasswordHash,FirstName,PaternalLastName,BirthDate,IsVerified) " +
                $"VALUES (2,N'{run}@example.invalid',N'TEST_NO_LOGIN',N'{run}',N'Test','2000-01-01',1); " +
                "SELECT CONVERT(INT,SCOPE_IDENTITY());"));
            productId = Convert.ToInt32(await Execute(connection, transaction,
                $"INSERT dbo.BaseProduct (ProducerId,CategoryId,Name,ShortDescription,MeasurementUnit) " +
                $"SELECT {producerId}, MIN(Id), N'{run}', N'Fixture', N'kg' FROM dbo.ProductCategory; " +
                "SELECT CONVERT(INT,SCOPE_IDENTITY());"));
            batchId = Convert.ToInt32(await Execute(connection, transaction,
                "INSERT dbo.Batch (BaseProductId,ProducerId,DeliveryPointId,BatchName,Description,MinimumGoal,Deadline,Status) " +
                $"VALUES ({productId},{producerId},{pointId},N'{run}',N'Fixture',1,DATEADD(DAY,7,GETUTCDATE()),N'Active'); " +
                "SELECT CONVERT(INT,SCOPE_IDENTITY());"));
            await transaction.CommitAsync();
        }
        try
        {
            foreach (string status in new[] { "Draft", "Active", "Confirmed", "Vigente", "Unknown" })
            {
                await using var update = new SqlCommand($"UPDATE dbo.Batch SET Status=N'{status}' WHERE Id={batchId}", connection);
                await update.ExecuteNonQueryAsync();
                await using var deactivate = new SqlCommand(
                    $"EXEC dbo.USP_SetDeliveryPointStatus @id={pointId}, @isActive=0", connection);
                var error = await Assert.ThrowsAsync<SqlException>(() => deactivate.ExecuteNonQueryAsync());
                Assert.Equal(50103, error.Number);
                await using var verify = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.DeliveryPoint d JOIN dbo.Batch b ON b.DeliveryPointId=d.Id " +
                    $"WHERE d.Id={pointId} AND d.IsActive=1 AND b.Id={batchId} AND b.Status=N'{status}'", connection);
                Assert.Equal(1, Convert.ToInt32(await verify.ExecuteScalarAsync()));
            }
            await using var cancel = new SqlCommand($"UPDATE dbo.Batch SET Status=N'Cancelled' WHERE Id={batchId}; " +
                $"EXEC dbo.USP_SetDeliveryPointStatus @id={pointId}, @isActive=0;", connection);
            await cancel.ExecuteNonQueryAsync();
            await using var revive = new SqlCommand($"UPDATE dbo.Batch SET Status=N'Active' WHERE Id={batchId}", connection);
            var blocked = await Assert.ThrowsAsync<SqlException>(() => revive.ExecuteNonQueryAsync());
            Assert.Equal(50103, blocked.Number);
        }
        finally
        {
            // Logical cleanup of known IDs owned by this run, preserving RN-07 associations.
            await using var cleanup = new SqlCommand(
                $"UPDATE dbo.Batch SET Status=N'Cancelled' WHERE Id={batchId} AND BatchName=N'{run}'; " +
                $"EXEC dbo.USP_SetDeliveryPointStatus @id={pointId}, @isActive=0; " +
                $"UPDATE dbo.BaseProduct SET IsActive=0 WHERE Id={productId} AND Name=N'{run}';", connection);
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    [DeliverySqlFact]
    public async Task TestCreateWithConcurrentDuplicateRequestsShouldPersistOnlyOnePoint()
    {
        string run = "TEST_" + Guid.NewGuid().ToString("N");
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<int> Attempt()
        {
            await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL"));
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            await start.Task;
            try
            {
                int id = await Create(connection, transaction, run);
                await transaction.CommitAsync();
                return id;
            }
            catch (SqlException exception) when (exception.Number is 2601 or 2627)
            {
                return 0;
            }
        }
        Task<int> first = Attempt();
        Task<int> second = Attempt();
        start.SetResult();
        int[] ids = await Task.WhenAll(first, second);
        await using var verifyConnection = new SqlConnection(Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL"));
        await verifyConnection.OpenAsync();
        try
        {
            Assert.Single(ids, id => id > 0);
            Assert.Single(ids, id => id == 0);
            await using var verify = new SqlCommand("SELECT COUNT(*) FROM dbo.DeliveryPoint WHERE Name=@name", verifyConnection);
            verify.Parameters.AddWithValue("@name", run);
            Assert.Equal(1, Convert.ToInt32(await verify.ExecuteScalarAsync()));
        }
        finally
        {
            foreach (int id in ids.Where(id => id > 0))
            {
                await using var cleanup = new SqlCommand(
                    "EXEC dbo.USP_SetDeliveryPointStatus @id=@id, @isActive=0", verifyConnection);
                cleanup.Parameters.AddWithValue("@id", id);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }

}
