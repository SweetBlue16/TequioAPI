using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tequio.Infrastructure.Models;
using Tequio.Domain.Constants;

namespace Tequio.Infrastructure.Data
{
    /// <summary>
    /// Seeds initial required data into the database securely without hardcoding credentials.
    /// </summary>
    public class DataSeeder
    {
        private readonly TequioDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public DataSeeder(TequioDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task SeedAdminAsync()
        {
            string email = _configuration["MasterAdmin:Email"] ?? string.Empty;
            string password = _configuration["MasterAdmin:Password"] ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Master admin credentials are not configured properly.");
            }

            bool adminExists = await _dbContext.Users.AnyAsync(u => u.Email == email);

            if (!adminExists)
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _dbContext.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO [dbo].[User]
                      ([RoleId], [Email], [PasswordHash], [FirstName], [PaternalLastName], [BirthDate], [IsVerified], [RegistrationDate]) 
                      VALUES 
                      (4, @email, @hash, 'Admin', 'Maestro', '2000-01-01', 1, GETUTCDATE())",
                    new SqlParameter("@email", email),
                    new SqlParameter("@hash", passwordHash)
                );
            }
        }
    }
}
