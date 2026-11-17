using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories;

/// <summary>Uses only stored procedures for delivery point persistence.</summary>
public sealed class DeliveryPointRepository(TequioDbContext context) : IDeliveryPointRepository
{
    /// <inheritdoc />
    public Task<DeliveryPointPageDto> ListAsync(int pageIndex, int pageSize)
    {
        return ExecuteAsync("USP_ListDeliveryPoint", async command =>
        {
            command.Parameters.Add(new SqlParameter("@pageIndex", pageIndex));
            command.Parameters.Add(new SqlParameter("@pageSize", pageSize));
            var count = new SqlParameter("@totalCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
            command.Parameters.Add(count);
            var points = new List<DeliveryPointDto>();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    points.Add(Read(reader));
                }
            }
            return new DeliveryPointPageDto(points, (int)count.Value, pageIndex, pageSize);
        });
    }

    /// <inheritdoc />
    public Task<DeliveryPointDto> GetAsync(int id)
    {
        return ExecuteAsync("USP_GetDeliveryPoint", async command =>
        {
            command.Parameters.Add(new SqlParameter("@id", id));
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new KeyNotFoundException("No se encontró el punto de entrega.");
            }
            return Read(reader);
        });
    }

    /// <inheritdoc />
    public Task<int> SaveAsync(int? id, SaveDeliveryPointDto dto)
    {
        return ExecuteAsync(id.HasValue ? "USP_UpdateDeliveryPoint" : "USP_CreateDeliveryPoint", async command =>
        {
            if (id.HasValue)
            {
                command.Parameters.Add(new SqlParameter("@id", id.Value));
            }
            foreach (var property in typeof(SaveDeliveryPointDto).GetProperties())
            {
                string name = "@" + char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                object value = property.GetValue(dto) ?? DBNull.Value;
                var parameter = new SqlParameter(name, value);
                if (property.Name is "Latitude" or "Longitude")
                {
                    parameter.SqlDbType = SqlDbType.Decimal;
                    parameter.Precision = 18;
                    parameter.Scale = 8;
                }
                command.Parameters.Add(parameter);
            }
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        });
    }

    /// <inheritdoc />
    public async Task SetStatusAsync(int id, bool isActive)
    {
        await ExecuteAsync("USP_SetDeliveryPointStatus", async command =>
        {
            command.Parameters.Add(new SqlParameter("@id", id));
            command.Parameters.Add(new SqlParameter("@isActive", isActive));
            return await command.ExecuteNonQueryAsync();
        });
    }

    private async Task<T> ExecuteAsync<T>(string procedure, Func<DbCommand, Task<T>> execute)
    {
        var connection = context.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;
        try
        {
            if (openedHere)
            {
                await connection.OpenAsync();
            }
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedure;
            return await execute(command);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627 or 50102)
        {
            throw new DeliveryPointConflictException("Ya existe un punto con ese nombre en el municipio.");
        }
        catch (SqlException exception) when (exception.Number == 50103)
        {
            throw new DeliveryPointConflictException("El punto tiene lotes asociados y no se puede desactivar.");
        }
        catch (SqlException exception) when (exception.Number == 50104)
        {
            throw new KeyNotFoundException("No se encontró el punto de entrega.");
        }
        catch (SqlException exception) when (exception.Number is 50101 or 50105)
        {
            throw new ArgumentException("Verifica los campos, las coordenadas y el municipio seleccionado.");
        }
        finally
        {
            if (openedHere && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static DeliveryPointDto Read(DbDataReader reader)
    {
        return new DeliveryPointDto
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Street = reader.GetString(reader.GetOrdinal("Street")),
            Number = reader.GetString(reader.GetOrdinal("Number")),
            Neighborhood = reader.GetString(reader.GetOrdinal("Neighborhood")),
            ZipCode = reader.GetString(reader.GetOrdinal("ZipCode")),
            City = reader.GetString(reader.GetOrdinal("City")),
            BusinessHours = reader.GetString(reader.GetOrdinal("BusinessHours")),
            References = reader.IsDBNull(reader.GetOrdinal("References"))
                ? null : reader.GetString(reader.GetOrdinal("References")),
            Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
            Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
            MunicipalityId = reader.GetInt32(reader.GetOrdinal("MunicipalityId")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
        };
    }
}
