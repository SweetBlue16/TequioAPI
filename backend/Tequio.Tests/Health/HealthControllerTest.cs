using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Tequio.Api.Controllers;
using Xunit;

namespace Tequio.Tests.Health
{
    /// <summary>
    /// Contains unit tests for <see cref="HealthController"/>.
    /// </summary>
    public class HealthControllerTest
    {
        private readonly HealthController _controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthControllerTest"/> class.
        /// </summary>
        public HealthControllerTest()
        {
            _controller = new HealthController(NullLogger<HealthController>.Instance);
        }

        /// <summary>
        /// Verifies that the health endpoint returns HTTP 200 OK with the expected Spanish message.
        /// </summary>
        [Fact]
        public void TestGetHealthWithActiveServiceShouldReturnOkWithStatusMessage()
        {
            IActionResult result = _controller.GetHealth();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            HealthStatusResponse payload = Assert.IsType<HealthStatusResponse>(okResult.Value);

            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal("Healthy", payload.Status);
            Assert.Equal("El servicio de Tequio está en línea y funcionando correctamente.", payload.Message);
        }

        /// <summary>
        /// Verifies that the health endpoint returns a valid UTC timestamp close to the current time.
        /// </summary>
        [Fact]
        public void TestGetHealthWithActiveServiceShouldReturnValidUtcTimestamp()
        {
            DateTime beforeCall = DateTime.UtcNow.AddSeconds(-1);

            IActionResult result = _controller.GetHealth();

            DateTime afterCall = DateTime.UtcNow.AddSeconds(1);
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            HealthStatusResponse payload = Assert.IsType<HealthStatusResponse>(okResult.Value);

            Assert.Equal(DateTimeKind.Utc, payload.TimestampUtc.Kind);
            Assert.InRange(payload.TimestampUtc, beforeCall, afterCall);
        }
    }
}