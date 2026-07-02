using Slotra.Api.UnitOfWork;

namespace Slotra.Api.Services;

public sealed class GenericService<TEntity>(IUnitOfWork unitOfWork) : IGenericService<TEntity>
    where TEntity : class
{
    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        unitOfWork.Repository<TEntity>().GetAllAsync(cancellationToken);

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        unitOfWork.Repository<TEntity>().GetByIdAsync(id, cancellationToken);

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await unitOfWork.Repository<TEntity>().AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> UpdateAsync(Guid id, Action<TEntity> updateEntity, CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.Repository<TEntity>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        updateEntity(entity);
        unitOfWork.Repository<TEntity>().Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.Repository<TEntity>().GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        unitOfWork.Repository<TEntity>().Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
