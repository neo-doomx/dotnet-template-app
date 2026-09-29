using Microsoft.EntityFrameworkCore;
using Template.Application.Abstractions.Persistence;
using Template.Domain.Entities;
using Template.Infrastructure.Persistence;

namespace Template.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken) =>
        _context.Products.AnyAsync(x => x.Name == name, cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
