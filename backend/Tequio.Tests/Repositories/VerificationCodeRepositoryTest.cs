using System.Data;
using Microsoft.EntityFrameworkCore;
using Tequio.Domain.Email;
using Tequio.Infrastructure.Models;
using Tequio.Infrastructure.Repositories;
using Xunit;

namespace Tequio.Tests.Repositories;

[Collection("DatabaseCollection")]
public class VerificationCodeRepositoryTest(DatabaseFixture fixture)
{
    #region SaveAsync

    [Fact]
    public async Task TestSaveWithValidDataShouldInsertVerificationCodeWithThreeAttempts()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var repository = new VerificationCodeRepository(context);
        const string code = "123456";

        await repository.SaveAsync(user.Id, code, SecurityCodeEmailType.AccountVerification);

        await using var assertContext = fixture.CreateDbContext();
        var savedCode = await assertContext.VerificationCodes
            .SingleOrDefaultAsync(vc => vc.UserId == user.Id && vc.Code == code);

        Assert.NotNull(savedCode);
        var expected = new
        {
            UserId = user.Id,
            Code = code,
            Type = nameof(SecurityCodeEmailType.AccountVerification),
            RemainingAttempts = 3
        };
        var actual = new
        {
            savedCode.UserId,
            savedCode.Code,
            savedCode.Type,
            savedCode.RemainingAttempts
        };
        Assert.Equal(expected, actual);
        Assert.True(savedCode.ExpirationDate > savedCode.CreationDate);
    }

    [Fact]
    public async Task TestSaveWhenActiveCodeAlreadyExistsShouldInvalidatePreviousAndInsertNew()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var previousCode = await SeedVerificationCodeAsync(
            context,
            user.Id,
            code: "111111",
            type: SecurityCodeEmailType.PasswordRecovery,
            remainingAttempts: 3);

        var repository = new VerificationCodeRepository(context);

        await repository.SaveAsync(user.Id, "222222", SecurityCodeEmailType.PasswordRecovery);

        await using var assertContext = fixture.CreateDbContext();
        var refreshedPrevious = await assertContext.VerificationCodes.FindAsync(previousCode.Id);
        var newCode = await assertContext.VerificationCodes
            .SingleOrDefaultAsync(vc => vc.UserId == user.Id && vc.Code == "222222");

        Assert.Equal(0, refreshedPrevious?.RemainingAttempts);
        Assert.Equal(3, newCode?.RemainingAttempts);
    }

    [Fact]
    public async Task TestSaveWhenUserDoesNotExistShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new VerificationCodeRepository(context);
        const int nonExistentUserId = -9999;

        // Violates FK_VerificationCode_User -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.SaveAsync(nonExistentUserId, "123456", SecurityCodeEmailType.AccountVerification));
    }

    [Fact]
    public async Task TestSaveWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var repository = new VerificationCodeRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.SaveAsync(user.Id, "123456", SecurityCodeEmailType.AccountVerification, cts.Token));
    }

    #endregion

    #region ValidateAsync

    [Fact]
    public async Task TestValidateWhenAccountVerificationCodeIsValidShouldConsumeCodeAndVerifyUser()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var verificationCode = await SeedVerificationCodeAsync(
            context,
            user.Id,
            code: "654321",
            type: SecurityCodeEmailType.AccountVerification,
            remainingAttempts: 3);

        var repository = new VerificationCodeRepository(context);

        await repository.ValidateAsync(user.Id, "654321", SecurityCodeEmailType.AccountVerification);

        await using var assertContext = fixture.CreateDbContext();
        var updatedCode = await assertContext.VerificationCodes.FindAsync(verificationCode.Id);
        var updatedUser = await assertContext.Users.FindAsync(user.Id);

        Assert.NotNull(updatedCode);
        Assert.NotNull(updatedUser);
        Assert.Equal(
            new { RemainingAttempts = 0, IsVerified = true },
            new { updatedCode.RemainingAttempts, updatedUser.IsVerified });
    }

    [Fact]
    public async Task TestValidateWhenNonAccountVerificationCodeIsValidShouldConsumeCodeWithoutVerifyingUser()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var verificationCode = await SeedVerificationCodeAsync(
            context,
            user.Id,
            code: "777888",
            type: SecurityCodeEmailType.PasswordRecovery,
            remainingAttempts: 3);

        var repository = new VerificationCodeRepository(context);

        await repository.ValidateAsync(user.Id, "777888", SecurityCodeEmailType.PasswordRecovery);

        await using var assertContext = fixture.CreateDbContext();
        var updatedCode = await assertContext.VerificationCodes.FindAsync(verificationCode.Id);
        var updatedUser = await assertContext.Users.FindAsync(user.Id);

        Assert.NotNull(updatedCode);
        Assert.NotNull(updatedUser);
        Assert.Equal(
            new { RemainingAttempts = 0, IsVerified = false },
            new { updatedCode.RemainingAttempts, updatedUser.IsVerified });
    }

    [Fact]
    public async Task TestValidateWhenNoCodeExistsShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var repository = new VerificationCodeRepository(context);

        // Raises ;THROW 50003 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.ValidateAsync(user.Id, "123456", SecurityCodeEmailType.AccountVerification));
    }

    [Fact]
    public async Task TestValidateWhenRemainingAttemptsIsZeroShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        await SeedVerificationCodeAsync(
            context,
            user.Id,
            code: "123456",
            type: SecurityCodeEmailType.AccountVerification,
            remainingAttempts: 0);

        var repository = new VerificationCodeRepository(context);

        // Raises ;THROW 50003 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.ValidateAsync(user.Id, "123456", SecurityCodeEmailType.AccountVerification));
    }

    [Fact]
    public async Task TestValidateWhenCodeIsExpiredShouldInvalidateCodeAndThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var expiredCode = await SeedVerificationCodeAsync(
            context,
            user.Id,
            code: "555555",
            type: SecurityCodeEmailType.TwoFactorAuthentication,
            remainingAttempts: 3,
            isExpired: true);

        var repository = new VerificationCodeRepository(context);

        // Raises ;THROW 50004 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.ValidateAsync(user.Id, "555555", SecurityCodeEmailType.TwoFactorAuthentication));

        await using var assertContext = fixture.CreateDbContext();
        var updatedCode = await assertContext.VerificationCodes.FindAsync(expiredCode.Id);
        Assert.NotNull(updatedCode);
        Assert.Equal(0, updatedCode.RemainingAttempts);
    }

    [Fact]
    public async Task TestValidateWhenCodeIsIncorrectShouldDecrementAttemptsAndThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false, roleId: 3); //roleId 3 = admin, USP_ValidateVerificationCode requires 
        var activeCode = await SeedVerificationCodeAsync(
            context,
            user.Id,
            code: "123456",
            type: SecurityCodeEmailType.AdminAuthorization,
            remainingAttempts: 3);

        var repository = new VerificationCodeRepository(context);

        // Raises ;THROW 50005 -> SqlExceptionMapper.Map
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.ValidateAsync(user.Id, "999999", SecurityCodeEmailType.AdminAuthorization));

        await using var assertContext = fixture.CreateDbContext();
        var updatedCode = await assertContext.VerificationCodes.FindAsync(activeCode.Id);
        Assert.NotNull(updatedCode);
        Assert.Equal(2, updatedCode.RemainingAttempts);
    }

    [Fact]
    public async Task TestValidateWhenConnectionIsAlreadyOpenShouldKeepConnectionOpen()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        await SeedVerificationCodeAsync(
            context,
            user.Id,
            code: "444444",
            type: SecurityCodeEmailType.TwoFactorAuthentication,
            remainingAttempts: 3);

        var repository = new VerificationCodeRepository(context);

        // Covers branch shouldCloseConnection == false at ExecuteWithConnectionAsync
        await context.Database.OpenConnectionAsync();

        await repository.ValidateAsync(user.Id, "444444", SecurityCodeEmailType.TwoFactorAuthentication);

        Assert.Equal(ConnectionState.Open, context.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task TestValidateWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, isVerified: false);
        var repository = new VerificationCodeRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.ValidateAsync(user.Id, "123456", SecurityCodeEmailType.AccountVerification, cts.Token));
    }

    #endregion

    #region Helpers

    private static async Task<User> SeedUserAsync(TequioDbContext context, bool isVerified, int roleId = 1)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var user = new User
        {
            RoleId = roleId,
            Email = $"verify_{uniqueId}@tequio.com",
            PasswordHash = "hashed_password_placeholder",
            FirstName = "Verificación",
            PaternalLastName = "Prueba",
            BirthDate = new DateOnly(1994, 7, 20),
            IsVerified = isVerified,
            RegistrationDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<VerificationCode> SeedVerificationCodeAsync(
        TequioDbContext context,
        int userId,
        string code,
        SecurityCodeEmailType type,
        int remainingAttempts,
        bool isExpired = false)
    {
        // We use a margin of days to avoid mismatches between DateTime.UtcNow and GETDATE() at the MsSQL container
        var verificationCode = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Type = type.ToString(),
            CreationDate = DateTime.UtcNow.AddDays(-2),
            ExpirationDate = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(1),
            RemainingAttempts = remainingAttempts
        };

        context.VerificationCodes.Add(verificationCode);
        await context.SaveChangesAsync();

        /* The test TestValidateWhenRemainingAttemptsIsZeroShouldThrowMappedException
         fails without the next segment: the supposed issue is that EF Core's scaffolding
         sets remaining attempts as .HasDefaultValue(3) (from RemainingAttempts INT NOT NULL DEFAULT 3)
         at 01_InitTequio.sql. When SeedVerificationCodeAsync inserts a VerificationCode with
         RemainingAttempts = 0, EF Core interprets 0 as if the field wasn't assigned (due to C#'s int
         default value), as a result SQL Server applies DEFAULT 3 instead of 0, so ValidateAsync at the
         repository approves the decrement to 2 without raising an exception.
        */
        if (remainingAttempts == 0)
        {
            await context.VerificationCodes
                .Where(v => v.Id == verificationCode.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.RemainingAttempts, 0));
            verificationCode.RemainingAttempts = 0;
        }

        return verificationCode;
    }

    #endregion
}