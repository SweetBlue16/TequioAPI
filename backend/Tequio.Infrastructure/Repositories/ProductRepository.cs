using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Infrastructure.Helpers;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories
{
    /// <summary>
    /// Executes product catalog database procedures using ADO.NET and centralized DbContext connection management.
    /// </summary>
    public sealed class ProductRepository : IProductRepository
    {
        private readonly TequioDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductRepository"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public ProductRepository(TequioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async Task<int> CreateBaseProductAsync(
            int producerId,
            CreateBaseProductDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                object? result = await ExecuteWithConnectionAsync(
                    async connection =>
                    {
                        await using DbCommand command = connection.CreateCommand();
                        command.CommandText = "USP_CreateBaseProduct";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(BuildCreateProductParameters(producerId, dto));

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
        public async Task DeactivateBaseProductAsync(
            int productId,
            int producerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await ExecuteWithConnectionAsync(
                    async connection =>
                    {
                        await using DbCommand command = connection.CreateCommand();
                        command.CommandText = "USP_DeactivateBaseProduct";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@productId", SqlDbType.Int) { Value = productId });
                        command.Parameters.Add(new SqlParameter("@producerId", SqlDbType.Int) { Value = producerId });

                        return await command.ExecuteNonQueryAsync(cancellationToken);
                    },
                    cancellationToken);
            }
            catch (SqlException exception)
            {
                throw SqlExceptionMapper.Map(exception);
            }
        }

        /// <inheritdoc />
        public async Task<ProductPageDto> GetProductsFromProducerAsync(
            int producerId,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await ExecuteWithConnectionAsync(
                    async connection =>
                    {
                        await using DbCommand command = connection.CreateCommand();
                        command.CommandText = "USP_GetProductsFromProducer";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@producerId", SqlDbType.Int) { Value = producerId });
                        command.Parameters.Add(new SqlParameter("@pageIndex", SqlDbType.Int) { Value = pageIndex });
                        command.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });

                        var totalCountParam = new SqlParameter("@totalCount", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(totalCountParam);

                        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                        var products = MapToProductDto(reader);
                        await reader.CloseAsync();

                        int totalCount = totalCountParam.Value != DBNull.Value
                            ? Convert.ToInt32(totalCountParam.Value, CultureInfo.InvariantCulture)
                            : 0;

                        return new ProductPageDto
                        {
                            PageIndex = pageIndex,
                            PageSize = pageSize,
                            ItemCount = totalCount,
                            Products = products
                        };
                    },
                    cancellationToken);
            }
            catch (SqlException exception)
            {
                throw SqlExceptionMapper.Map(exception);
            }
        }

        /// <inheritdoc />
        public async Task<ProductPageDto> GetProductsByCategoryAsync(
            int categoryId,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await ExecuteWithConnectionAsync(
                    async connection =>
                    {
                        await using DbCommand command = connection.CreateCommand();
                        command.CommandText = "USP_GetProductsByCategory";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@categoryId", SqlDbType.Int) { Value = categoryId });
                        command.Parameters.Add(new SqlParameter("@pageIndex", SqlDbType.Int) { Value = pageIndex });
                        command.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });

                        var totalCountParam = new SqlParameter("@totalCount", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(totalCountParam);

                        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                        var products = MapToProductDto(reader);
                        await reader.CloseAsync();

                        int totalCount = totalCountParam.Value != DBNull.Value
                            ? Convert.ToInt32(totalCountParam.Value, CultureInfo.InvariantCulture)
                            : 0;

                        return new ProductPageDto
                        {
                            PageIndex = pageIndex,
                            PageSize = pageSize,
                            ItemCount = totalCount,
                            Products = products
                        };
                    },
                    cancellationToken);
            }
            catch (SqlException exception)
            {
                throw SqlExceptionMapper.Map(exception);
            }
        }

        /// <summary>
        /// Reads records from data reader projecting rows into ProductDto models.
        /// </summary>
        private static List<ProductDto> MapToProductDto(DbDataReader reader)
        {
            var products = new List<ProductDto>();

            while (reader.Read())
            {
                products.Add(new ProductDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ProducerId = reader.GetInt32(reader.GetOrdinal("ProducerId")),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    ShortDescription = reader.GetString(reader.GetOrdinal("ShortDescription")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("ImageUrl")),
                    MeasurementUnit = reader.GetString(reader.GetOrdinal("MeasurementUnit")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }

            return products;
        }

        /// <summary>
        /// Constructs parameterized database arguments for base product registration.
        /// </summary>
        private static SqlParameter[] BuildCreateProductParameters(int producerId, CreateBaseProductDto dto)
        {
            return
            [
                new SqlParameter("@producerId", SqlDbType.Int) { Value = producerId },
                new SqlParameter("@categoryId", SqlDbType.Int) { Value = dto.CategoryId },
                new SqlParameter("@name", SqlDbType.NVarChar, 150) { Value = dto.Name },
                new SqlParameter("@shortDescription", SqlDbType.NVarChar, 255) { Value = dto.ShortDescription },
                new SqlParameter("@imageUrl", SqlDbType.NVarChar, 500)
                {
                    Value = string.IsNullOrWhiteSpace(dto.ImageUrl) ? DBNull.Value : dto.ImageUrl
                },
                new SqlParameter("@measurementUnit", SqlDbType.NVarChar, 50) { Value = dto.MeasurementUnit }
            ];
        }

        /// <summary>
        /// Manages the opening, execution, and safe closure of the database connection.
        /// </summary>
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
}