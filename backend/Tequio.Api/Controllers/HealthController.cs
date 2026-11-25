using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Provides health check endpoints to verify that the Tequio API service is online.
    /// </summary>
    [ApiController]
    [Route("api/v1/health")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for health check diagnostics.</param>
        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HealthStatusResponse), StatusCodes.Status200OK)]
        public IActionResult GetHealth()
        {
            DateTime utcNow = DateTime.UtcNow;
            _logger.LogInformation("Verificación de estado del servicio Tequio ejecutada a las {TimestampUtc}.", utcNow);

            HealthStatusResponse response = new HealthStatusResponse
            {
                Status = "Healthy",
                Message = "El servicio de Tequio está en línea y funcionando correctamente.",
                TimestampUtc = utcNow
            };
            return Ok(response);
        }
    }

    /// <summary>
    /// Represents the payload returned by the health check endpoint.
    /// </summary>
    public class HealthStatusResponse
    {
        /// <summary>
        /// Gets or sets the operational status indicator.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable status message in Spanish.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the health check was processed.
        /// </summary>
        public DateTime TimestampUtc { get; set; }
    }
}
