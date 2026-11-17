using Microsoft.Data.SqlClient;
using Tequio.Tests.DeliveryPoints;
using Xunit;

namespace Tequio.Tests.Auth;

/// <summary>Real SQL authorization invariants using only unique, isolated TEST_ accounts.</summary>
public sealed class AdminAuthorizationSqlTest
{
    private static async Task WithAdmin(Func<SqlConnection, int, Task> test)
    {
        await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL"));
        await connection.OpenAsync();
        await using var create = new SqlCommand(
            "INSERT dbo.[User](RoleId,Email,PasswordHash,FirstName,PaternalLastName,BirthDate,IsVerified) " +
            "OUTPUT INSERTED.Id VALUES(3,@email,N'TEST_NO_LOGIN',N'TEST_OTP',N'Test','1990-01-01',0)", connection);
        create.Parameters.AddWithValue("@email", "TEST_" + Guid.NewGuid().ToString("N") + "@example.invalid");
        int id = Convert.ToInt32(await create.ExecuteScalarAsync());
        try
        {
            await test(connection, id);
        }
        finally
        {
            await Execute(connection, id, "UPDATE dbo.VerificationCode SET RemainingAttempts=0 WHERE UserId=@id");
        }
    }

    private static async Task<object?> Execute(SqlConnection connection, int id, string query)
    {
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteScalarAsync();
    }

    private static Task<object?> Generate(SqlConnection c, int id) => Execute(c, id,
        "EXEC dbo.USP_GenerateVerificationCode @userId=@id,@code=N'123456',@type=N'AdminAuthorization'");
    private static Task<object?> Validate(SqlConnection c, int id, string code = "123456") => Execute(c, id,
        $"EXEC dbo.USP_ValidateVerificationCode @userId=@id,@code=N'{code}',@type=N'AdminAuthorization'");
    private static async Task<bool> Verified(SqlConnection c, int id) => Convert.ToBoolean(
        await Execute(c, id, "SELECT IsVerified FROM dbo.[User] WHERE Id=@id"));

    [DeliverySqlFact]
    public Task TestAuthorizeWithValidCodeShouldAtomicallyVerifyAndConsumeOnce() => WithAdmin(async (c, id) =>
    {
        await Generate(c, id);
        Assert.False(await Verified(c, id));
        await Validate(c, id);
        Assert.True(await Verified(c, id));
        Assert.Equal(0, Convert.ToInt32(await Execute(c, id,
            "SELECT TOP(1) RemainingAttempts FROM dbo.VerificationCode WHERE UserId=@id ORDER BY Id DESC")));
        Assert.Equal(50003, (await Assert.ThrowsAsync<SqlException>(() => Validate(c, id))).Number);
    });

    [DeliverySqlFact]
    public Task TestAuthorizeWithWrongCodesShouldConsumeAttemptsAndPreventResendReset() => WithAdmin(async (c, id) =>
    {
        await Generate(c, id);
        for (int remaining = 2; remaining >= 0; remaining--)
        {
            Assert.Equal(50005, (await Assert.ThrowsAsync<SqlException>(() => Validate(c, id, "000000"))).Number);
            Assert.Equal(remaining, Convert.ToInt32(await Execute(c, id,
                "SELECT TOP(1) RemainingAttempts FROM dbo.VerificationCode WHERE UserId=@id ORDER BY Id DESC")));
        }
        Assert.False(await Verified(c, id));
        Assert.Equal(50021, (await Assert.ThrowsAsync<SqlException>(() => Generate(c, id))).Number);
        Assert.Equal(50003, (await Assert.ThrowsAsync<SqlException>(() => Validate(c, id))).Number);
    });

    [DeliverySqlFact]
    public Task TestAuthorizeWithExpiredCodeShouldRemainPendingAndAllowFreshChallenge() => WithAdmin(async (c, id) =>
    {
        await Generate(c, id);
        await Execute(c, id, "UPDATE dbo.VerificationCode SET CreationDate=DATEADD(MINUTE,-16,GETDATE()), " +
            "ExpirationDate=DATEADD(MINUTE,-1,GETDATE()) WHERE UserId=@id");
        Assert.Equal(50004, (await Assert.ThrowsAsync<SqlException>(() => Validate(c, id))).Number);
        Assert.False(await Verified(c, id));
        await Generate(c, id);
        await Validate(c, id);
        Assert.True(await Verified(c, id));
    });

    [DeliverySqlFact]
    public Task TestResendWithLiveChallengeShouldRetainRemainingAttemptsAndInvalidatePreviousCode() => WithAdmin(async (c, id) =>
    {
        await Generate(c, id);
        await Assert.ThrowsAsync<SqlException>(() => Validate(c, id, "000000"));
        await Execute(c, id, "UPDATE dbo.VerificationCode SET CreationDate=DATEADD(SECOND,-61,GETDATE()) WHERE UserId=@id");
        await Execute(c, id,
            "EXEC dbo.USP_GenerateVerificationCode @userId=@id,@code=N'654321',@type=N'AdminAuthorization'");
        Assert.Equal(2, Convert.ToInt32(await Execute(c, id,
            "SELECT TOP(1) RemainingAttempts FROM dbo.VerificationCode WHERE UserId=@id ORDER BY Id DESC")));
        Assert.Equal(0, Convert.ToInt32(await Execute(c, id,
            "SELECT RemainingAttempts FROM dbo.VerificationCode WHERE UserId=@id AND Code=N'123456'")));
        Assert.Equal(50005, (await Assert.ThrowsAsync<SqlException>(() => Validate(c, id))).Number);
        Assert.False(await Verified(c, id));
    });

    [DeliverySqlFact]
    public Task TestResendWithExcessiveRequestsShouldEnforceCooldownAndWindow() => WithAdmin(async (c, id) =>
    {
        await Generate(c, id);
        Assert.Equal(50020, (await Assert.ThrowsAsync<SqlException>(() => Generate(c, id))).Number);
        for (int count = 0; count < 2; count++)
        {
            await Execute(c, id, "UPDATE dbo.VerificationCode SET CreationDate=DATEADD(SECOND,-61,GETDATE()) WHERE UserId=@id");
            await Generate(c, id);
        }
        await Execute(c, id, "UPDATE dbo.VerificationCode SET CreationDate=DATEADD(SECOND,-61,GETDATE()) WHERE UserId=@id");
        Assert.Equal(50021, (await Assert.ThrowsAsync<SqlException>(() => Generate(c, id))).Number);
        Assert.False(await Verified(c, id));
    });

    [DeliverySqlFact]
    public Task TestPublicVerificationWithAdminShouldRejectWithoutConsumingAuthorizationCode() => WithAdmin(async (c, id) =>
    {
        await Generate(c, id);
        var error = await Assert.ThrowsAsync<SqlException>(() => Execute(c, id,
            "EXEC dbo.USP_ValidateVerificationCode @userId=@id,@code=N'123456',@type=N'AccountVerification'"));
        Assert.Equal(50003, error.Number);
        Assert.False(await Verified(c, id));
        Assert.Equal(3, Convert.ToInt32(await Execute(c, id,
            "SELECT RemainingAttempts FROM dbo.VerificationCode WHERE UserId=@id")));
        Assert.Equal(50003, (await Assert.ThrowsAsync<SqlException>(() => Execute(c, id,
            "EXEC dbo.USP_GenerateVerificationCode @userId=@id,@code=N'123456',@type=N'AccountVerification'"))).Number);
    });

    [DeliverySqlFact]
    public Task TestGenerateWithConcurrentRequestsShouldIssueOnlyOneCode() => WithAdmin(async (c, id) =>
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<int> Attempt()
        {
            await using var other = new SqlConnection(Environment.GetEnvironmentVariable("TEQUIO_TEST_SQL"));
            await other.OpenAsync();
            await start.Task;
            try
            {
                await Generate(other, id);
                return 0;
            }
            catch (SqlException exception) when (exception.Number == 50020)
            {
                return exception.Number;
            }
        }
        var first = Attempt();
        var second = Attempt();
        start.SetResult();
        var results = await Task.WhenAll(first, second);
        Assert.Single(results, result => result == 0);
        Assert.Single(results, result => result == 50020);
        Assert.Equal(1, Convert.ToInt32(await Execute(c, id,
            "SELECT COUNT(*) FROM dbo.VerificationCode WHERE UserId=@id")));
    });
}
