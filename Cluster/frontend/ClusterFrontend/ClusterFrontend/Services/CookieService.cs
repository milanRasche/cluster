using ClusterFrontend.Interface;

namespace ClusterFrontend.Services
{
    public class CookieService : ICookieService
    {
        public void SetSecureCookie(HttpContext httpContext, string key, string value, int days)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(days)
            };

            httpContext.Response.Cookies.Append(key, value, options);
        }

        public string GetCookie(HttpContext httpContext, string key)
        {
            if (httpContext.Request.Cookies.TryGetValue(key, out string value))
            {
                return value;
            }
            return null;
        }

        public void RemoveCookie(HttpContext httpContext, string key)
        {
            httpContext.Response.Cookies.Delete(key);
        }

        public void SetJwtToken(HttpContext httpContext, string token, int days = 1)
        {
            SetSecureCookie(httpContext, "JWTToken", token, days);
        }

        public void SetRefreshToken(HttpContext httpContext, string token, int days = 7)
        {
            SetSecureCookie(httpContext, "RefreshToken", token, days);
        }

        public string GetJwtToken(HttpContext httpContext)
        {
            return GetCookie(httpContext, "JWTToken");
        }

        public string GetRefreshToken(HttpContext httpContext)
        {
            return GetCookie(httpContext, "RefreshToken");
        }

        public void RemoveAuthTokens(HttpContext httpContext)
        {
            RemoveCookie(httpContext, "JWTToken");
            RemoveCookie(httpContext, "RefreshToken");
        }
    }
}
