using Backend.Models.Sql;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Backend.Exceptions;

namespace Backend.DataAccess;

public class UserDAL : IUserDAL
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserDAL> _logger;

    public UserDAL(ApplicationDbContext context, ILogger<UserDAL> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            _logger.LogInformation($"Creating new user with email: {user.Email}");

            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw new AuthDataAccessException("Error creating user", ex);
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by ID: {userId}");
            throw new AuthDataAccessException($"Error getting user by ID: {userId}", ex);
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by email: {email}");
            throw new AuthDataAccessException($"Error getting user by email: {email}", ex);
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by username: {username}");
            throw new AuthDataAccessException($"Error getting user by username: {username}", ex);
        }
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking if user exists by email: {email}");
            throw new AuthDataAccessException($"Error checking if user exists by email: {email}", ex);
        }
    }

    public async Task<bool> UserExistsByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking if user exists by username: {username}");
            throw new AuthDataAccessException($"Error checking if user exists by username: {username}", ex);
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating user: {user.UserId}");
            throw new AuthDataAccessException($"Error updating user: {user.UserId}", ex);
        }
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new AuthDataAccessException($"Error updating last login for user: {userId}", ex);
        }
    }
}