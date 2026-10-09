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
                50006 => new ArgumentException(ErrorMessages.NegativePageIndex),
                50007 => new ArgumentException(ErrorMessages.InvalidPageSize),
                50008 => new ArgumentException(ErrorMessages.PageSizeExceeded),
                50009 => new ArgumentException(ErrorMessages.NegativePageIndex),
                50010 => new ArgumentException(ErrorMessages.InvalidPageSize),
                50011 => new ArgumentException(ErrorMessages.PageSizeExceeded),
                50012 => new ArgumentException(ErrorMessages.ProducerNotFoundOrUnauthorized),
                50013 => new ArgumentException(ErrorMessages.CategoryNotFound),
                50014 => new ArgumentException(ErrorMessages.DuplicateProductName),
                50015 => new ArgumentException(ErrorMessages.ProductNotFoundOrForbidden),
                50016 => new InvalidOperationException(ErrorMessages.ProductAlreadyDeactivated),
                50017 => new InvalidOperationException(ErrorMessages.ProductHasActiveBatches),
                _ => new InvalidOperationException(ErrorMessages.DatabaseError)
            };
        }
    }
}
