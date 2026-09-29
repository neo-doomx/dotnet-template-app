using Microsoft.EntityFrameworkCore;
using Template.Application.Abstractions.Persistence;
using Template.Domain.Entities;
using Template.Infrastructure.Persistence;

namespace Template.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
        _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
        _context.Users.AnyAsync(x => x.Username == username, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
