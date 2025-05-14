namespace Auth.API.Interfaces
{
    public interface IRefreshTokenGenerator
    {
        string GenerateToken();
    }
}
