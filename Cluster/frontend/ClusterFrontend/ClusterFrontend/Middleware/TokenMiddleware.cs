using ClusterFrontend.DTOs;
using ClusterFrontend.Interface;
using System.Text.Json;

namespace ClusterFrontend.Middleware
{
    public class TokenMiddleware(
        RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, IAuthService authService, ICookieService cookieService)
        {
            if (context.Request.Path == "/auth/UserAuth/login" && context.Request.Method == "POST")
            {
                context.Request.EnableBuffering();

                try
                {
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    var loginRequest = JsonSerializer.Deserialize<UserLoginRequest>(body);
                    if (loginRequest != null)
                    {
                        var tokens = await authService.Login(loginRequest);
                        if (tokens != null)
                        {
                            cookieService.SetJwtToken(context, tokens.JWTToken);
                            cookieService.SetRefreshToken(context, tokens.RefreshToken);                        }
                        else
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("Invalid email or password");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"An error occurred: {ex.Message}");
                    return;
                }
            }

            await _next(context);
        }
    }
}
