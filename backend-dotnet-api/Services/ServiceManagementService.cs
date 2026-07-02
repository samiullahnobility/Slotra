using Microsoft.EntityFrameworkCore;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Common;
using Slotra.Api.DTOs.Services;
using Slotra.Api.Models;
using Slotra.Api.UnitOfWork;

namespace Slotra.Api.Services;

public sealed class ServiceManagementService(IUnitOfWork unitOfWork) : IServiceManagementService
{
    public async Task<IReadOnlyList<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var services = await unitOfWork.Repository<Service>()
            .Query()
            .OrderBy(service => service.Name)
            .ToListAsync(cancellationToken);

        return services.Select(ToResponse).ToList();
    }

    public async Task<PagedResponse<ServiceResponse>> GetPagedAsync(QueryPageRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = unitOfWork.Repository<Service>().Query().OrderBy(service => service.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(service => ToResponse(service))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ServiceResponse>(items, page, pageSize, total);
    }

    public async Task<ServiceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
        return service is null ? null : ToResponse(service);
    }

    public async Task<ServiceResult<ServiceResponse>> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request.Name, request.DurationMinutes, request.Price);
        if (validationError is not null)
        {
            return ServiceResult<ServiceResponse>.ValidationError(validationError);
        }

        var name = request.Name.Trim();
        var nameExists = await unitOfWork.Repository<Service>()
            .AnyAsync(service => service.Name == name, cancellationToken);

        if (nameExists)
        {
            return ServiceResult<ServiceResponse>.Conflict("A service with that name already exists.");
        }

        var service = new Service
        {
            Name = name,
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            IsActive = request.IsActive
        };

        await unitOfWork.Repository<Service>().AddAsync(service, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServiceResponse>.Success(ToResponse(service));
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request.Name, request.DurationMinutes, request.Price);
        if (validationError is not null)
        {
            return ServiceResult.ValidationError(validationError);
        }

        var service = await unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
        if (service is null)
        {
            return ServiceResult.NotFound();
        }

        var name = request.Name.Trim();
        var nameExists = await unitOfWork.Repository<Service>()
            .AnyAsync(existing => existing.Id != id && existing.Name == name, cancellationToken);

        if (nameExists)
        {
            return ServiceResult.Conflict("A service with that name already exists.");
        }

        service.Name = name;
        service.Description = request.Description?.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.IsActive = request.IsActive;

        unitOfWork.Repository<Service>().Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await unitOfWork.Repository<Service>().GetByIdAsync(id, cancellationToken);
        if (service is null)
        {
            return ServiceResult.NotFound();
        }

        var hasAppointments = await unitOfWork.Repository<Appointment>()
            .AnyAsync(appointment => appointment.ServiceId == id, cancellationToken);

        if (hasAppointments)
        {
            service.IsActive = false;
            unitOfWork.Repository<Service>().Update(service);
        }
        else
        {
            unitOfWork.Repository<Service>().Remove(service);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static string? Validate(string name, int durationMinutes, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Service name is required.";
        }

        if (durationMinutes <= 0)
        {
            return "Service duration must be greater than zero.";
        }

        if (price < 0)
        {
            return "Service price cannot be negative.";
        }

        return null;
    }

    private static ServiceResponse ToResponse(Service service) =>
        new(service.Id, service.Name, service.Description, service.DurationMinutes, service.Price, service.IsActive);
}
