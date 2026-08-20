using Streetcode.BLL.DTO.Authentication;
using Streetcode.DAL.Enums;

namespace Streetcode.BLL.Interfaces.Jwt
{
    public interface IJwtService
    {
        AuthTokenResult GenerateToken(int userId, string email, UserRole role);
    }
}
