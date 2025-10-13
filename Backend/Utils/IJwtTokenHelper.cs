using Backend.Models.Sql;

namespace Backend.Utils;

public interface IJwtTokenHelper
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    Guid GetUserIdFromToken(string token);
}