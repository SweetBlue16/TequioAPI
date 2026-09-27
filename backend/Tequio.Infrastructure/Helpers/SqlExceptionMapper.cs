using Microsoft.Data.SqlClient;
using Tequio.Domain.Constants;

namespace Tequio.Infrastructure.Helpers
{
    /// <summary>
    /// Utility class to map SQL Server error numbers to Domain exceptions.
    /// </summary>
    public static class SqlExceptionMapper
    {
        public static Exception Map(SqlException exception)
        {
            return exception.Number switch
            {
                50001 => new ArgumentException(ErrorMessages.AgeRestriction),
                50002 => new ArgumentException(ErrorMessages.EmailAlreadyRegistered),
                50003 => new ArgumentException(ErrorMessages.OtpInvalid),
                50004 => new ArgumentException(ErrorMessages.OtpExpired),
                50005 => new ArgumentException(ErrorMessages.OtpInvalid),
                _ => new InvalidOperationException(ErrorMessages.DatabaseError)
            };
        }
    }
}
