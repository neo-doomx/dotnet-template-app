using Template.Domain.Entities;

namespace Template.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
