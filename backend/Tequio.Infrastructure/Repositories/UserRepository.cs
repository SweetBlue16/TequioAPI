using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tequio.Domain.Repositories;
using Tequio.Infrastructure.Helpers;
using Tequio.Infrastructure.Models;

namespace Tequio.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of user profile data access using Entity Framework and Stored Procedures.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly TequioDbContext _dbContext;

        public UserRepository(TequioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task UpdateProfilePictureAsync(int userId, string profilePictureUrl)
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC USP_UpdateUserProfilePicture @userId, @profilePictureUrl",
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@profilePictureUrl", profilePictureUrl)
                );
            }
            catch (SqlException ex)
            {
                throw SqlExceptionMapper.Map(ex);
            }
        }
    }
}
