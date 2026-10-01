using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Globalization;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Models;
using Tequio.Domain.Repositories;
using Tequio.Infrastructure.Helpers;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories;

/// <summary>
/// Executes authentication-related database procedures.
/// </summary>
public sealed class AuthRepository : IAuthRepository
{
    private readonly TequioDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthRepository"/> class.
    /// </summary>
    /// <param name="dbContext">Application database context.</param>
    public AuthRepository(TequioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<int> CreateUserAsync(
        RegisterRequestDto request,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            object? result = await ExecuteWithConnectionAsync(
                async connection =>
                {
                    await using DbCommand command = connection.CreateCommand();
                    command.CommandText = "USP_CreateUser";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@roleId", request.RoleId));
                    command.Parameters.Add(new SqlParameter("@email", request.Email));
                    command.Parameters.Add(new SqlParameter("@passwordHash", passwordHash));
                    command.Parameters.Add(new SqlParameter("@firstName", request.FirstName));
                    command.Parameters.Add(new SqlParameter("@paternalLastName", request.PaternalLastName));
                    command.Parameters.Add(
                        new SqlParameter("@maternalLastName", request.MaternalLastName ?? (object)DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@birthDate", request.BirthDate));
                    command.Parameters.Add(
                        new SqlParameter("@phoneNumber", request.PhoneNumber ?? (object)DBNull.Value));

                    return await command.ExecuteScalarAsync(cancellationToken);
                },
                cancellationToken);

            if (result is null || result is DBNull)
            {
                throw new InvalidOperationException(ErrorMessages.DatabaseError);
            }

            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        catch (SqlException exception)
        {
            throw SqlExceptionMapper.Map(exception);
        }
    }

    /// <inheritdoc />
    public async Task<(string? UserId, string? RoleId, string? PasswordHash)> GetUserAuthDataAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteWithConnectionAsync<(string? UserId, string? RoleId, string? PasswordHash)>(
                async connection =>
                {
                    await using DbCommand command = CreateGetUserByEmailCommand(connection, email);
                    await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return (
                            UserId: (string?)null,
                            RoleId: (string?)null,
                            PasswordHash: (string?)null);
                    }

                    string passwordHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                    string userId = reader.GetInt32(reader.GetOrdinal("Id"))
                        .ToString(CultureInfo.InvariantCulture);
                    string roleId = reader.GetInt32(reader.GetOrdinal("RoleId"))
                        .ToString(CultureInfo.InvariantCulture);

                    return (userId, roleId, passwordHash);
                },
                cancellationToken);
        }
        catch (SqlException exception)
        {
            throw SqlExceptionMapper.Map(exception);
        }
    }

    /// <inheritdoc />
    public async Task<UserVerificationData?> GetUserVerificationDataAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteWithConnectionAsync(
                async connection =>
                {
                    await using DbCommand command = CreateGetUserByEmailCommand(connection, email);
                    await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return new UserVerificationData(
                        reader.GetInt32(reader.GetOrdinal("Id")),
                        reader.GetString(reader.GetOrdinal("FirstName")),
                        reader.GetBoolean(reader.GetOrdinal("IsVerified")));
                },
                cancellationToken);
        }
        catch (SqlException exception)
        {
            throw SqlExceptionMapper.Map(exception);
        }
    }

    private static DbCommand CreateGetUserByEmailCommand(DbConnection connection, string email)
    {
        DbCommand command = connection.CreateCommand();
        command.CommandText = "USP_GetUserByEmail";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@email", email));
        return command;
    }

    private async Task<TResult> ExecuteWithConnectionAsync<TResult>(
        Func<DbConnection, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        DbConnection connection = _dbContext.Database.GetDbConnection();
        bool shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            return await operation(connection);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
