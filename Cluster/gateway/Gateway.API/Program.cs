using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);
var AllowedOrigins = "_allowedOrigins";

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

app.UseRouting();
app.UseHttpLogging(); 
app.UseCors(AllowedOrigins);
app.MapReverseProxy();


app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/tasks/swagger/v1/swagger.json", "Tasks API");
    c.SwaggerEndpoint("/users/swagger/v1/swagger.json", "Users API");
    c.SwaggerEndpoint("/dashboard/swagger/v1/swagger.json", "Dashboard API");
    c.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "Auth  API");

    c.RoutePrefix = "";
});

app.Run();

