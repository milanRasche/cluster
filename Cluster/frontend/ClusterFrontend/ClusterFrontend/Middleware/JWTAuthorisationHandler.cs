using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterFrontend.Middleware
{
    public class JWTAuthorisationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                if (httpContext.Request.Cookies.TryGetValue("JWTToken", out string jwtToken) &&
                    !string.IsNullOrWhiteSpace(jwtToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                    Console.WriteLine($"JWT token attached from cookie for request to {request.RequestUri}");
                }
                else
                {
                    Console.WriteLine($"No JWT token found in cookie for request to {request.RequestUri}");
                }
            }
            else
            {
                Console.WriteLine("HttpContext is null, cannot retrieve JWT token from cookie");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}