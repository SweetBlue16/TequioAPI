using Microsoft.EntityFrameworkCore;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Infrastructure.Models;
using Tequio.Infrastructure.Repositories;
using Xunit;

namespace Tequio.Tests.Repositories;

[Collection("DatabaseCollection")]
public class UserRepositoryTest(DatabaseFixture fixture)
{
    [Fact]
    public async Task TestGetUserProfileWhenUserExistsWithAllFieldsShouldReturnFullDto()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: true);
        var repository = new UserRepository(context);

        var result = await repository.GetUserProfileAsync(user.Id);

        Assert.NotNull(result);
        var expected = new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.PaternalLastName,
            user.MaternalLastName,
            user.BirthDate,
            user.PhoneNumber,
            user.Locality,
            user.Biography,
            user.ProfilePictureUrl,
            user.IsVerified
        };
        var actual = new
        {
            result.Id,
            result.Email,
            result.FirstName,
            result.PaternalLastName,
            result.MaternalLastName,
            BirthDate = DateOnly.FromDateTime(result.BirthDate),
            result.PhoneNumber,
            result.Locality,
            result.Biography,
            result.ProfilePictureUrl,
            result.IsVerified
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestGetUserProfileWhenUserExistsWithNullOptionalFieldsShouldReturnDtoWithNulls()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: false);
        var repository = new UserRepository(context);

        var result = await repository.GetUserProfileAsync(user.Id);

        Assert.NotNull(result);
        var expected = new
        {
            user.Id,
            MaternalLastName = (string?)null,
            PhoneNumber = (string?)null,
            Locality = (string?)null,
            Biography = (string?)null,
            ProfilePictureUrl = (string?)null
        };
        var actual = new
        {
            result.Id,
            result.MaternalLastName,
            result.PhoneNumber,
            result.Locality,
            result.Biography,
            result.ProfilePictureUrl
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestGetUserProfileWhenUserDoesNotExistShouldThrowKeyNotFoundException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new UserRepository(context);
        const int nonExistentUserId = -9999;

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.GetUserProfileAsync(nonExistentUserId));

        Assert.Equal(ErrorMessages.RecordNotFound, ex.Message);
    }

    [Fact]
    public async Task TestGetUserProfileWhenSqlExceptionOccursShouldThrowMappedException()
    {
        // Use a DbContext with a non-existing DB on the same instance
        // to force an SqlException without altering the schema.
        var brokenConnectionString = fixture.ConnectionString + ";Initial Catalog=NonExistentDb_Test";
        var options = new DbContextOptionsBuilder<TequioDbContext>()
            .UseSqlServer(brokenConnectionString)
            .Options;

        await using var brokenContext = new TequioDbContext(options);
        var repository = new UserRepository(brokenContext);

        // Replace the Exception for the specific type returned by SqlExceptionMapper.Map(ex)
        await Assert.ThrowsAnyAsync<Exception>(() => repository.GetUserProfileAsync(1));
    }

    [Fact]
    public async Task TestUpdateProfilePictureWithValidUrlShouldUpdateDatabase()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: false);
        var repository = new UserRepository(context);
        const string newPictureUrl = "https://cdn.tequio.com/avatars/user-updated.jpg";

        await repository.UpdateProfilePictureAsync(user.Id, newPictureUrl);

        // Use a new context to avoid reading the EFCore's cache 
        await using var assertContext = fixture.CreateDbContext();
        var updatedUser = await assertContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(newPictureUrl, updatedUser.ProfilePictureUrl);
    }

    [Fact]
    public async Task TestUpdateProfilePictureWhenSqlExceptionOccursShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: false);
        var repository = new UserRepository(context);

        // Using null (instead of DBNull.Value) makes SqlClient skip the parameter
        // so SQL Server throws a SqlException (Error 201: missing required parameter).
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.UpdateProfilePictureAsync(user.Id, null!));
    }

    [Fact]
    public async Task TestUpdateUserProfileWithAllOptionalFieldsShouldUpdateDatabase()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: false);
        var repository = new UserRepository(context);

        var request = new UpdateUserProfileDto
        {
            FirstName = "Elena",
            PaternalLastName = "Morales",
            MaternalLastName = "Ríos",
            BirthDate = new DateTime(1995, 5, 20, 0, 0, 0, DateTimeKind.Local),
            PhoneNumber = "2281234567",
            Locality = "Xalapa",
            Biography = "Productora de café de altura."
        };

        await repository.UpdateUserProfileAsync(user.Id, request);

        await using var assertContext = fixture.CreateDbContext();
        var updatedUser = await assertContext.Users.FindAsync(user.Id);

        Assert.NotNull(updatedUser);
        var expected = new
        {
            request.FirstName,
            request.PaternalLastName,
            MaternalLastName = (string?)request.MaternalLastName,
            BirthDate = DateOnly.FromDateTime(request.BirthDate),
            PhoneNumber = (string?)request.PhoneNumber,
            Locality = (string?)request.Locality,
            Biography = (string?)request.Biography
        };
        var actual = new
        {
            updatedUser.FirstName,
            updatedUser.PaternalLastName,
            updatedUser.MaternalLastName,
            updatedUser.BirthDate,
            updatedUser.PhoneNumber,
            updatedUser.Locality,
            updatedUser.Biography
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestUpdateUserProfileWithNullOptionalFieldsShouldClearOptionalColumns()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: true);
        var repository = new UserRepository(context);

        var request = new UpdateUserProfileDto
        {
            FirstName = "Carlos",
            PaternalLastName = "Vega",
            MaternalLastName = null,
            BirthDate = new DateTime(1990, 10, 15, 0, 0, 0, DateTimeKind.Local),
            PhoneNumber = null,
            Locality = null,
            Biography = null
        };

        await repository.UpdateUserProfileAsync(user.Id, request);

        await using var assertContext = fixture.CreateDbContext();
        var updatedUser = await assertContext.Users.FindAsync(user.Id);

        Assert.NotNull(updatedUser);
        var expected = new
        {
            request.FirstName,
            request.PaternalLastName,
            MaternalLastName = (string?)null,
            PhoneNumber = (string?)null,
            Locality = (string?)null,
            Biography = (string?)null
        };
        var actual = new
        {
            updatedUser.FirstName,
            updatedUser.PaternalLastName,
            updatedUser.MaternalLastName,
            updatedUser.PhoneNumber,
            updatedUser.Locality,
            updatedUser.Biography
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TestUpdateUserProfileWhenSqlExceptionOccursShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: false);
        var repository = new UserRepository(context);

        var invalidRequest = new UpdateUserProfileDto
        {
            FirstName = null!, // Throws SqlException (Error 201) for lack of DBNull.Value fallback
            PaternalLastName = "Vega",
            BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.UpdateUserProfileAsync(user.Id, invalidRequest));
    }
    
    [Fact]
    public async Task TestPromoteToProducerWhenUserIsNotProducerShouldUpdateRoleIdToTwo()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: false, roleId: 1);
        var repository = new UserRepository(context);

        await repository.PromoteToProducerAsync(user.Id);

        await using var assertContext = fixture.CreateDbContext();
        var updatedUser = await assertContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(2, updatedUser.RoleId);
    }

    [Fact]
    public async Task TestPromoteToProducerWhenUserDoesNotExistShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new UserRepository(context);
        const int nonExistentUserId = -9999;

        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.PromoteToProducerAsync(nonExistentUserId));
    }

    [Fact]
    public async Task TestPromoteToProducerWhenUserIsAlreadyProducerShouldThrowMappedException()
    {
        await using var context = fixture.CreateDbContext();
        var producerUser = await SeedUserAsync(context, includeOptionalFields: false, roleId: 2);
        var repository = new UserRepository(context);

        // (;THROW 50019 -> SqlExceptionMapper.Map)
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.PromoteToProducerAsync(producerUser.Id));
    }

    [Fact]
    public async Task TestPromoteToProducerWhenCancellationRequestedShouldThrowOperationCanceledException()
    {
        await using var context = fixture.CreateDbContext();
        var user = await SeedUserAsync(context, includeOptionalFields: false, roleId: 1);
        var repository = new UserRepository(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.PromoteToProducerAsync(user.Id, cts.Token));
    }

    private static async Task<User> SeedUserAsync(
        TequioDbContext context,
        bool includeOptionalFields,
        int roleId = 1)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var user = new User
        {
            RoleId = roleId,
            Email = $"test_{uniqueId}@tequio.com",
            PasswordHash = "hashed_password_placeholder",
            FirstName = "Juan",
            PaternalLastName = "Pérez",
            MaternalLastName = includeOptionalFields ? "López" : null,
            BirthDate = new DateOnly(1992, 3, 10),
            PhoneNumber = includeOptionalFields ? "2289876543" : null,
            Locality = includeOptionalFields ? "Coatepec" : null,
            Biography = includeOptionalFields ? "Productor local de miel." : null,
            ProfilePictureUrl = includeOptionalFields ? "https://cdn.tequio.com/avatars/default.jpg" : null,
            IsVerified = true,
            RegistrationDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}