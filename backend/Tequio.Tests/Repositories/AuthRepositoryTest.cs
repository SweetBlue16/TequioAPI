using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tequio.Domain.Dtos;
using Tequio.Domain.Models;
using Tequio.Infrastructure.Models;
using Tequio.Infrastructure.Repositories;
using Xunit;

namespace Tequio.Tests.Repositories;

[Collection("DatabaseCollection")]
public class AuthRepositoryTest(DatabaseFixture fixture)
{
    #region CreateUserAsync

    [Fact]
    public async Task TestCreateUserWithAllOptionalFieldsShouldInsertUserAndReturnNewId()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        const string passwordHash = "hashed_secret_123";

        var request = new RegisterRequestDto
        {
            RoleId = 1,
            Email = $"create_full_{uniqueId}@tequio.com",
            FirstName = "Sofía",
            PaternalLastName = "Gómez",
            MaternalLastName = "Ruiz",
            BirthDate = new DateTime(1995, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            PhoneNumber = "2281112233"
        };

        var newUserId = await repository.CreateUserAsync(request, passwordHash);

        await using var assertContext = fixture.CreateDbContext();
        var createdUser = await assertContext.Users.FindAsync(newUserId);

        Assert.True(newUserId > 0);
        Assert.NotNull(createdUser);
        var expected = new
        {
            request.RoleId,
            request.Email,
            PasswordHash = passwordHash,
            request.FirstName,
            request.PaternalLastName,
            MaternalLastName = (string?)request.MaternalLastName,
            BirthDate = DateOnly.FromDateTime(request.BirthDate),
            PhoneNumber = (string?)request.PhoneNumber
        };
        var actual = new
        {
            createdUser.RoleId,
            createdUser.Email,
            createdUser.PasswordHash,
            createdUser.FirstName,
            createdUser.PaternalLastName,
            createdUser.MaternalLastName,
            createdUser.BirthDate,
            createdUser.PhoneNumber
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestCreateUserWithNullOptionalFieldsShouldInsertUserWithNulls()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        const string passwordHash = "hashed_secret_456";

        var request = new RegisterRequestDto
        {
            RoleId = 1,
            Email = $"create_null_{uniqueId}@tequio.com",
            FirstName = "Luis",
            PaternalLastName = "Mendoza",
            MaternalLastName = null,
            BirthDate = new DateTime(1990, 8, 12, 0, 0, 0, DateTimeKind.Utc),
            PhoneNumber = null
        };

        var newUserId = await repository.CreateUserAsync(request, passwordHash);

        await using var assertContext = fixture.CreateDbContext();
        var createdUser = await assertContext.Users.FindAsync(newUserId);

        Assert.True(newUserId > 0);
        Assert.NotNull(createdUser);
        var expected = new
        {
            request.Email,
            MaternalLastName = (string?)null,
            PhoneNumber = (string?)null
        };
        var actual = new
        {
            createdUser.Email,
            createdUser.MaternalLastName,
            createdUser.PhoneNumber
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestCreateUserWhenUserIsUnderageShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var request = new RegisterRequestDto
        {
            RoleId = 1,
            Email = $"underage_{uniqueId}@tequio.com",
            FirstName = "Mateo",
            PaternalLastName = "Silva",
            BirthDate = DateTime.UtcNow.AddYears(-17) // Raises ;THROW 50001
        };

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.CreateUserAsync(request, "hashed_password"));
    }

    [Fact]
    public async Task TestCreateUserWhenEmailAlreadyExistsShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var existingUser = await SeedUserAsync(context);
        var repository = new AuthRepository(context);

        var request = new RegisterRequestDto
        {
            RoleId = 1,
            Email = existingUser.Email, // Raises ;THROW 50002
            FirstName = "Duplicado",
            PaternalLastName = "Prueba",
            BirthDate = new DateTime(1992, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.CreateUserAsync(request, "hashed_password"));
    }

    [Fact]
    public async Task TestCreateUserWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var request = new RegisterRequestDto
        {
            RoleId = 1,
            Email = $"cancel_{Guid.NewGuid():N}@tequio.com",
            FirstName = "Cancelado",
            PaternalLastName = "Test",
            BirthDate = new DateTime(1992, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.CreateUserAsync(request, "hashed_password", cts.Token));
    }

    #endregion

    #region GetUserAuthDataAsync

    [Fact]
    public async Task TestGetUserAuthDataWhenUserExistsShouldReturnUserAuthTuple()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, roleId: 2, isVerified: true);
        var repository = new AuthRepository(context);

        var result = await repository.GetUserAuthDataAsync(user.Email);

        var expected = (
            UserId: (string?)user.Id.ToString(CultureInfo.InvariantCulture),
            RoleId: (string?)user.RoleId.ToString(CultureInfo.InvariantCulture),
            PasswordHash: (string?)user.PasswordHash,
            user.IsVerified
        );
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task TestGetUserAuthDataWhenUserDoesNotExistShouldReturnEmptyTuple()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);

        var result = await repository.GetUserAuthDataAsync("nonexistent_auth@tequio.com");

        Assert.Equal(((string?)null, (string?)null, (string?)null, false), result);
    }

    [Fact]
    public async Task TestGetUserAuthDataWhenSqlExceptionOccursShouldThrowMappedException()
    {
        await using var brokenContext = CreateBrokenContext();
        var repository = new AuthRepository(brokenContext);

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.GetUserAuthDataAsync("any@tequio.com"));
    }

    [Fact]
    public async Task TestGetUserAuthDataWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetUserAuthDataAsync("any@tequio.com", cts.Token));
    }

    #endregion

    #region GetUserVerificationDataAsync

    [Fact]
    public async Task TestGetUserVerificationDataWhenUserExistsShouldReturnUserVerificationData()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, roleId: 2, isVerified: false);
        var repository = new AuthRepository(context);

        var result = await repository.GetUserVerificationDataAsync(user.Email);

        var expected = new UserVerificationData(user.Id, user.FirstName, user.IsVerified, user.RoleId);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task TestGetUserVerificationDataWhenUserDoesNotExistShouldReturnNull()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);

        var result = await repository.GetUserVerificationDataAsync("nonexistent_verify@tequio.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task TestGetUserVerificationDataWhenSqlExceptionOccursShouldThrowMappedException()
    {
        await using var brokenContext = CreateBrokenContext();
        var repository = new AuthRepository(brokenContext);

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.GetUserVerificationDataAsync("any@tequio.com"));
    }

    [Fact]
    public async Task TestGetUserVerificationDataWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetUserVerificationDataAsync("any@tequio.com", cts.Token));
    }

    #endregion

    #region UpdatePasswordAsync

    [Fact]
    public async Task TestUpdatePasswordWithValidDataShouldUpdatePasswordHashInDatabase()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context);
        var repository = new AuthRepository(context);
        const string newPasswordHash = "new_hashed_password_789";

        await repository.UpdatePasswordAsync(user.Id, newPasswordHash);

        await using var assertContext = fixture.CreateDbContext();
        var updatedUser = await assertContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(newPasswordHash, updatedUser.PasswordHash);
    }

    [Fact]
    public async Task TestUpdatePasswordWhenSqlExceptionOccursShouldThrowMappedException()
    {
        await using var brokenContext = CreateBrokenContext();
        var repository = new AuthRepository(brokenContext);

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.UpdatePasswordAsync(1, "new_hashed_password"));
    }

    [Fact]
    public async Task TestUpdatePasswordWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context);
        var repository = new AuthRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.UpdatePasswordAsync(user.Id, "new_hashed_password", cts.Token));
    }

    #endregion

    #region UserExistsAsync

    [Fact]
    public async Task TestUserExistsWhenEmailExistsShouldReturnTrue()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context);
        var repository = new AuthRepository(context);

        var exists = await repository.UserExistsAsync(user.Email);

        Assert.True(exists);
    }

    [Fact]
    public async Task TestUserExistsWhenEmailDoesNotExistShouldReturnFalse()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);

        var exists = await repository.UserExistsAsync("missing_user@tequio.com");

        Assert.False(exists);
    }

    [Fact]
    public async Task TestUserExistsWhenConnectionIsAlreadyOpenShouldKeepConnectionOpen()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context);
        var repository = new AuthRepository(context);

        // Covers branch shouldCloseConnection == false at ExecuteWithConnectionAsync
        await context.Database.OpenConnectionAsync();

        var exists = await repository.UserExistsAsync(user.Email);

        Assert.True(exists);
        Assert.Equal(ConnectionState.Open, context.Database.GetDbConnection().State);
    }

    [Fact]
    public async Task TestUserExistsWhenSqlExceptionOccursShouldThrowMappedException()
    {
        await using var brokenContext = CreateBrokenContext();
        var repository = new AuthRepository(brokenContext);

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.UserExistsAsync("any@tequio.com"));
    }

    [Fact]
    public async Task TestUserExistsWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new AuthRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.UserExistsAsync("any@tequio.com", cts.Token));
    }

    #endregion

    #region Helpers

    private TequioDbContext CreateBrokenContext()
    {
        var brokenConnectionString = fixture.ConnectionString + ";Initial Catalog=NonExistentDb_Test";
        var options = new DbContextOptionsBuilder<TequioDbContext>()
            .UseSqlServer(brokenConnectionString)
            .Options;

        return new TequioDbContext(options);
    }

    private static async Task<User> SeedUserAsync(
        TequioDbContext context,
        int roleId = 1,
        bool isVerified = true)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var user = new User
        {
            RoleId = roleId,
            Email = $"auth_{uniqueId}@tequio.com",
            PasswordHash = "initial_hashed_password",
            FirstName = "Ana",
            PaternalLastName = "Torres",
            MaternalLastName = "Cruz",
            BirthDate = new DateOnly(1993, 6, 15),
            PhoneNumber = "2280001122",
            IsVerified = isVerified,
            RegistrationDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    #endregion
}