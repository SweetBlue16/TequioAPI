using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;
using Tequio.Infrastructure.Helpers;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of product data access
    /// </summary>
    public class ProductRepository(TequioDbContext dbContext) : IProductRepository
    {
        public async Task<ProductPageDto> GetProductsFromProducerAsync(int producerId, int pageIndex, int pageSize)
        {
            try
            {
                await using var command = dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = "USP_GetProductsFromProducer";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@producerId", producerId));
                command.Parameters.Add(new SqlParameter("@pageIndex", pageIndex));
                command.Parameters.Add(new SqlParameter("@pageSize", pageSize));

                var totalCountParam = new SqlParameter("@totalCount", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(totalCountParam);

                await dbContext.Database.OpenConnectionAsync();
                await using var reader = await command.ExecuteReaderAsync();

                var products = MapToProductDto(reader);

                await reader.CloseAsync();

                var totalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

                return new ProductPageDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    ItemCount = totalCount,
                    Products = products
                };
            }
            catch (SqlException ex)
            {
                throw SqlExceptionMapper.Map(ex);
            }
        }

        public async Task<ProductPageDto> GetProductsByCategoryAsync(int categoryId, int pageIndex, int pageSize)
        {
            try
            {
                await using var command = dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = "USP_GetProductsByCategory";
                command.CommandType = CommandType.StoredProcedure;
    
                command.Parameters.Add(new SqlParameter("@categoryId", categoryId));
                command.Parameters.Add(new SqlParameter("@pageIndex", pageIndex));
                command.Parameters.Add(new SqlParameter("@pageSize", pageSize));

                var totalCountParam = new SqlParameter("@totalCount", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(totalCountParam);

                await dbContext.Database.OpenConnectionAsync();
                await using var reader = await command.ExecuteReaderAsync();

                var products = MapToProductDto(reader);

                await reader.CloseAsync();

                var totalCount = totalCountParam.Value != DBNull.Value ? (int)totalCountParam.Value : 0;

                return new ProductPageDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    ItemCount = totalCount,
                    Products = products
                };
            } catch (SqlException ex)
            {
                throw SqlExceptionMapper.Map(ex);
            }
        }

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
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                    MeasurementUnit = reader.GetString(reader.GetOrdinal("MeasurementUnit")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }

            return products;
        }
    }
}
