using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers;

/// <summary>Shared physical catalog; only administrators manage its records (RN-06).</summary>
[ApiController]
[Route("api/v1/delivery-points")]
public sealed class DeliveryPointController(IDeliveryPointService service) : ControllerBase
{
    /// <summary>Lists the shared catalog without restricting buyer or producer queries.</summary>
    [HttpGet]
    public async Task<IActionResult> ListAsync(int pageIndex = 1, int pageSize = 10)
    {
        return Ok(await service.ListAsync(pageIndex, pageSize));
    }

    /// <summary>Retrieves the persisted physical delivery location.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsync(int id)
    {
        return Ok(await service.GetAsync(id));
    }

    /// <summary>Creates a physical delivery point as Administrator.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAsync(SaveDeliveryPointDto dto)
    {
        int id = await service.SaveAsync(null, dto);
        return Created($"/api/v1/delivery-points/{id}", new { id, mensaje = "Punto de entrega registrado." });
    }

    /// <summary>Updates editable fields without changing availability.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAsync(int id, SaveDeliveryPointDto dto)
    {
        await service.SaveAsync(id, dto);
        return Ok(new { mensaje = "Punto de entrega actualizado." });
    }

    /// <summary>Deactivates only when RN-07 permits it.</summary>
    [HttpPatch("{id:int}/deactive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateAsync(int id)
    {
        await service.SetStatusAsync(id, false);
        return Ok(new { mensaje = "Punto de entrega desactivado." });
    }

    /// <summary>Restores availability without deleting associations.</summary>
    [HttpPatch("{id:int}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateAsync(int id)
    {
        await service.SetStatusAsync(id, true);
        return Ok(new { mensaje = "Punto de entrega activado." });
    }
}
