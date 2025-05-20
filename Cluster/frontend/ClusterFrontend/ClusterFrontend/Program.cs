using ClusterFrontend.Components;
using ClusterFrontend.Interface;
using ClusterFrontend.Middleware;
using ClusterFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpContextAccessor must be registered before handlers that depend on it
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register JWTAuthorisationHandler after HttpContextAccessor
builder.Services.AddTransient<JWTAuthorisationHandler>();

// Register the HttpClient with the handler
builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "ClusterFrontend");
})
.AddHttpMessageHandler<JWTAuthorisationHandler>();

// Register other services
builder.Services.AddScoped<IRunnerService, RunnerService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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