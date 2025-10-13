using Backend.Models;
using System.Threading.Tasks;
using Backend.DTOs.Auth;

namespace Backend.Business;

public interface IAuthBL
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<bool> LogoutAsync(string token);
    Task<bool> ValidateTokenAsync(string token);
}