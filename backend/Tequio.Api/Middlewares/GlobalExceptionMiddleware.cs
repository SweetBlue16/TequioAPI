using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;
using Tequio.Domain.Constants;

namespace Tequio.Api.Middlewares
{
    /// <summary>
    /// Global middleware to intercept specific exceptions and format HTTP responses.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ArgumentException ex)
            {
                await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleExceptionAsync(context, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (SqlException)
            {
                await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// Formats and writes the exception response as JSON.
        /// </summary>
        private static Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                error = message
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
