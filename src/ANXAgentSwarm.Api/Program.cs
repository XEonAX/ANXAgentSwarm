using ANXAgentSwarm.Api.Hubs;
using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

// Add Infrastructure services (repositories, DbContext, Ollama provider, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Register SessionHub broadcaster (bridges Infrastructure with API layer)
builder.Services.AddScoped<ISessionHubBroadcaster, SessionHubBroadcaster>();

// Add CORS for frontend development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Initialize database and seed data
await app.Services.InitializeDatabaseAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<SessionHub>("/hubs/session");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
