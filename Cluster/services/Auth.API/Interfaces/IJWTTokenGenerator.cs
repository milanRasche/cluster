using Auth.API.Objects;

namespace Auth.API.Interfaces
{
    public interface IJWTTokenGenerator
    {
        string GenerateToken(User user);
    }
}
