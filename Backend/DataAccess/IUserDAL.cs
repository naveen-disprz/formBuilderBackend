using Backend.Models.Sql;
using System.Threading.Tasks;

namespace Backend.DataAccess;

public interface IUserDAL
{
    // Create
    Task<User> CreateUserAsync(User user);

    // Read
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> UserExistsByEmailAsync(string email);
    Task<bool> UserExistsByUsernameAsync(string username);

    // Update
    Task<User> UpdateUserAsync(User user);
    Task UpdateLastLoginAsync(Guid userId);

}