using Microsoft.AspNetCore.HttpLogging;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        // If using a development certificate, accept any certificate for incoming requests
        // This is for development only!
        httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.NoCertificate;
        httpsOptions.CheckCertificateRevocation = false;
    });
});

// Setup HTTPS client handler for outgoing requests
builder.Services.ConfigureHttpClientDefaults(httpClient =>
{
    // For development, we'll ignore certificate validation for the reverse proxy
    httpClient.ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });
});

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.All;
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

//app.UseHttpsRedirection();
app.UseRouting();
app.UseHttpLogging();
app.UseCors();
app.MapReverseProxy();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/tasks/swagger/v1/swagger.json", "Tasks API");
    c.SwaggerEndpoint("/users/swagger/v1/swagger.json", "Users API");
    c.SwaggerEndpoint("/dashboard/swagger/v1/swagger.json", "Dashboard API");
    c.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "Auth API");
    c.RoutePrefix = "";
});

app.Run();