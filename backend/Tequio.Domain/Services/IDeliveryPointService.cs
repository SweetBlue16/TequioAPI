using System.ComponentModel.DataAnnotations;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;

namespace Tequio.Domain.Services;

/// <summary>Delivery point application contract.</summary>
public interface IDeliveryPointService
{
    Task<DeliveryPointPageDto> ListAsync(int pageIndex, int pageSize);
    Task<DeliveryPointDto> GetAsync(int id);
    Task<int> SaveAsync(int? id, SaveDeliveryPointDto dto);
    Task SetStatusAsync(int id, bool isActive);
}

/// <summary>Validates inputs before dispatching persistence to stored procedures.</summary>
public sealed class DeliveryPointService(IDeliveryPointRepository repository) : IDeliveryPointService
{
    /// <inheritdoc />
    public Task<DeliveryPointPageDto> ListAsync(int pageIndex, int pageSize)
    {
        if (pageIndex < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("La página debe ser positiva y su tamaño entre 1 y 100.");
        }
        return repository.ListAsync(pageIndex, pageSize);
    }

    /// <inheritdoc />
    public Task<DeliveryPointDto> GetAsync(int id)
    {
        ValidateId(id);
        return repository.GetAsync(id);
    }

    /// <inheritdoc />
    public Task<int> SaveAsync(int? id, SaveDeliveryPointDto dto)
    {
        if (id.HasValue)
        {
            ValidateId(id.Value);
        }
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
        {
            throw new ArgumentException(string.Join(" ", errors.Select(error => error.ErrorMessage)));
        }
        return repository.SaveAsync(id, dto);
    }

    /// <inheritdoc />
    public Task SetStatusAsync(int id, bool isActive)
    {
        ValidateId(id);
        return repository.SetStatusAsync(id, isActive);
    }

    private static void ValidateId(int id)
    {
        if (id < 1)
        {
            throw new ArgumentException("El identificador debe ser positivo.");
        }
    }
}
