namespace ClusterFrontend.Interface
{
    public interface ICookieService
    {
        public void SetSecureCookie(HttpContent httpContext, string key, string value, int days);
    }
}
