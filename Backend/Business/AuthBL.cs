using Backend.Models.Sql;
using System;
using System.Threading.Tasks;
using Backend.Business;
using Backend.DataAccess;
using Backend.DTOs.Auth;
using Backend.Enums;
using Backend.Utils;
using Backend.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Backend.Business
{
    public class AuthBL : IAuthBL
    {
        private readonly IUserDAL _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenHelper _jwtTokenHelper;
        private readonly ILogger<AuthBL> _logger;

        public AuthBL(
            IUserDAL userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenHelper jwtTokenService,
            ILogger<AuthBL> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenHelper = jwtTokenService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                _logger.LogInformation($"Registering new user: {registerDto.Email}");

                // Validate input
                if (string.IsNullOrWhiteSpace(registerDto.Email) ||
                    string.IsNullOrWhiteSpace(registerDto.Username) ||
                    string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    throw new ValidationException("Email, username, and password are required");
                }

                // Check if user already exists
                if (await _userRepository.UserExistsByEmailAsync(registerDto.Email))
                {
                    throw new UserAlreadyExistsException("User with this email already exists");
                }

                if (await _userRepository.UserExistsByUsernameAsync(registerDto.Username))
                {
                    throw new UserAlreadyExistsException("Username is already taken");
                }

                // Validate password strength
                if (registerDto.Password.Length < 6)
                {
                    throw new PasswordPolicyException("Password must be at least 6 characters long");
                }

                // Create new user
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = registerDto.Email.ToLower(),
                    Username = registerDto.Username,
                    PasswordHash = _passwordHasher.HashPassword(registerDto.Password),
                    Role = UserRole.Learner,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save to database
                var createdUser = await _userRepository.CreateUserAsync(user);

                // Generate JWT token
                var token = _jwtTokenHelper.GenerateToken(createdUser);

                _logger.LogInformation($"User registered successfully: {createdUser.Email}");

                return new AuthResponseDto
                {
                    Success = true,
                    Token = token,
                    UserId = createdUser.UserId,
                    Email = createdUser.Email,
                    Username = createdUser.Username,
                    Role = createdUser.Role.ToString(),
                    Message = "Registration successful",
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
            }
            catch (AuthException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                throw;
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Login attempt for: {loginDto.EmailOrUsername}");

                // Validate input
                if (string.IsNullOrWhiteSpace(loginDto.EmailOrUsername) ||
                    string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    throw new ValidationException("Email/Username and password are required");
                }

                // Find user by email or username
                User? user = null;

                if (loginDto.EmailOrUsername.Contains("@"))
                {
                    user = await _userRepository.GetUserByEmailAsync(loginDto.EmailOrUsername);
                }
                else
                {
                    user = await _userRepository.GetUserByUsernameAsync(loginDto.EmailOrUsername);
                }

                if (user == null)
                {
                    throw new AuthenticationFailedException("Invalid credentials");
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    throw new AuthenticationFailedException("Invalid credentials");
                }

                // Update last login
                await _userRepository.UpdateLastLoginAsync(user.UserId);

                // Generate JWT token
                var token = _jwtTokenHelper.GenerateToken(user);

                _logger.LogInformation($"User logged in successfully: {user.Email}");

                return new AuthResponseDto
                {
                    Success = true,
                    Token = token,
                    UserId = user.UserId,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role.ToString(),
                    Message = "Login successful",
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
            }
            catch (AuthException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                throw;
            }
        }

        public async Task<bool> LogoutAsync(string token)
        {
            try
            {
                _logger.LogInformation("Processing logout request");

                // In a real implementation, you might want to:
                // 1. Add token to a blacklist (Redis/Database)
                // 2. Clear any server-side sessions
                // 3. Log the logout event

                // For now, we'll just return true
                // The client should remove the token from storage

                await Task.CompletedTask; // Placeholder for async operations

                _logger.LogInformation("User logged out successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                // Validate token
                var isValid = _jwtTokenHelper.ValidateToken(token);

                if (isValid)
                {
                    // Optionally check if token is blacklisted
                    // var isBlacklisted = await CheckTokenBlacklist(token);
                    // return !isBlacklisted;
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }
    }
}
