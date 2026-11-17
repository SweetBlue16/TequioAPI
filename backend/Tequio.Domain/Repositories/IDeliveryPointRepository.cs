using Tequio.Domain.Dtos;

namespace Tequio.Domain.Repositories;

/// <summary>Stored procedure boundary for the shared delivery catalog.</summary>
public interface IDeliveryPointRepository
{
    Task<DeliveryPointPageDto> ListAsync(int pageIndex, int pageSize);
    Task<DeliveryPointDto> GetAsync(int id);
    Task<int> SaveAsync(int? id, SaveDeliveryPointDto dto);
    Task SetStatusAsync(int id, bool isActive);
}
