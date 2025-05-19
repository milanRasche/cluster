namespace ClusterFrontend.Interface
{
    public interface ICookieService
    {
        void SetSecureCookie(HttpContext httpContext, string key, string value, int days);
        string GetCookie(HttpContext httpContext, string key);
        void RemoveCookie(HttpContext httpContext, string key);
        void SetJwtToken(HttpContext httpContext, string token, int days = 1);
        void SetRefreshToken(HttpContext httpContext, string token, int days = 7);
        string GetJwtToken(HttpContext httpContext);
        string GetRefreshToken(HttpContext httpContext);
        void RemoveAuthTokens(HttpContext httpContext);
    }
}
