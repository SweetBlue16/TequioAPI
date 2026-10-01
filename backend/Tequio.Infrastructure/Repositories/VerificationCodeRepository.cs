using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Tequio.Domain.Constants;
using Tequio.Domain.Email;
using Tequio.Domain.Repositories;
using Tequio.Infrastructure.Helpers;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories;

/// <summary>
/// Executes the database procedures that persist and validate verification codes.
/// </summary>
public sealed class VerificationCodeRepository : IVerificationCodeRepository
{
    private readonly TequioDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationCodeRepository"/> class.
    /// </summary>
    /// <param name="dbContext">Application database context.</param>
    public VerificationCodeRepository(TequioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        int userId,
        string verificationCode,
        SecurityCodeEmailType type,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteWithConnectionAsync(
                async connection =>
                {
                    await using DbCommand command = connection.CreateCommand();
                    command.CommandText = "USP_GenerateVerificationCode";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@userId", userId));
                    command.Parameters.Add(new SqlParameter("@code", verificationCode));
                    command.Parameters.Add(new SqlParameter("@type", type.ToString()));

                    await command.ExecuteNonQueryAsync(cancellationToken);
                },
                cancellationToken);
        }
        catch (SqlException exception)
        {
            throw SqlExceptionMapper.Map(exception);
        }
    }

    /// <inheritdoc />
    public async Task ValidateAsync(
        int userId,
        string verificationCode,
        SecurityCodeEmailType type,
        CancellationToken cancellationToken = default)
    {
        try
        {
            object? result = await ExecuteWithConnectionAsync(
                async connection =>
                {
                    await using DbCommand command = connection.CreateCommand();
                    command.CommandText = "USP_ValidateVerificationCode";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@userId", userId));
                    command.Parameters.Add(new SqlParameter("@code", verificationCode));
                    command.Parameters.Add(new SqlParameter("@type", type.ToString()));

                    return await command.ExecuteScalarAsync(cancellationToken);
                },
                cancellationToken);

            if (result is null || result is DBNull || Convert.ToInt32(result) != 1)
            {
                throw new InvalidOperationException(ErrorMessages.DatabaseError);
            }
        }
        catch (SqlException exception)
        {
            throw SqlExceptionMapper.Map(exception);
        }
    }

    private async Task ExecuteWithConnectionAsync(
        Func<DbConnection, Task> operation,
        CancellationToken cancellationToken)
    {
        await ExecuteWithConnectionAsync<object?>(
            async connection =>
            {
                await operation(connection);
                return null;
            },
            cancellationToken);
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
