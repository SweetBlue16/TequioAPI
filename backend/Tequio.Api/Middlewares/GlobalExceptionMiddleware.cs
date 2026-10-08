using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;
using Tequio.Domain.Constants;
using Tequio.Domain.Email;

namespace Tequio.Api.Middlewares
{
    /// <summary>
    /// Global middleware to intercept specific exceptions and format HTTP responses.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación incorrecta: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso denegado: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (EmailDeliveryException ex)
            {
                _logger.LogError(ex, "Fallo al enviar correo electrónico.");
                await HandleExceptionAsync(context, HttpStatusCode.ServiceUnavailable, ex.Message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error a nivel de base de datos.");
                await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, ErrorMessages.DatabaseError);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operación inválida en el servidor.");
                await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, ErrorMessages.DatabaseError);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Recurso no encontrado: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno no controlado durante la petición.");
                await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.");
            }
        }

        /// <summary>
        /// Formats and writes the exception response as JSON.
        /// </summary>
        private static Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
