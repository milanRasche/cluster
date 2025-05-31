using ClusterFrontend.Components;
using ClusterFrontend.Interface;
using ClusterFrontend.Middleware;
using ClusterFrontend.Objects;
using ClusterFrontend.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpContextAccessor must be registered before handlers that depend on it
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register JWTAuthorisationHandler after HttpContextAccessor
builder.Services.AddTransient<JWTAuthorisationHandler>();

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

//IAuthService HTTP Client
builder.Services.AddHttpClient<IAuthService, AuthService>((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
        client.BaseAddress = new Uri(opts.AuthApiBaseUrl);
        client.DefaultRequestHeaders.Add("User-Agent", "ClusterFrontend");
    });

//IRunnerService HTTP Client
builder.Services.AddHttpClient<IRunnerService, RunnerService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    client.BaseAddress = new Uri(opts.AuthApiBaseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "ClusterFrontend");
})
.AddHttpMessageHandler<JWTAuthorisationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();