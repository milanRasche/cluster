using ClusterFrontend.DTOs;

namespace ClusterFrontend.Interface
{
    public interface IAuthService
    {
        Task<bool> Register(UserRegisterRequest registerRequest);
        Task<AuthResponse?> Login(UserLoginRequest loginRequest);
    }
}
