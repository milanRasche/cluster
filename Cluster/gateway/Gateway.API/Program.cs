var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapReverseProxy();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/tasks/swagger/v1/swagger.json", "Tasks API");
    c.SwaggerEndpoint("/users/swagger/v1/swagger.json", "Users API");
    c.SwaggerEndpoint("/dashboard/swagger/v1/swagger.json", "Dashboard API");
    c.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "Users API");

    c.RoutePrefix = "";
});

app.Run();

