using Template.Domain.Entities;

namespace Template.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);
}
