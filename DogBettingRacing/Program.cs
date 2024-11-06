using DogBettingRacing.Data;
using DogBettingRacing.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddHostedService<SimulationService>();
builder.Services.AddHostedService<BetService>();
builder.Services.AddHostedService<SimService>();
builder.Services.AddHttpClient();
builder.Services.AddLogging();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application has started successfully");

using (var scope = app.Services.CreateScope())
{
    var simService = scope.ServiceProvider.GetService<SimService>();
    var betService = scope.ServiceProvider.GetService<BetService>();
}

/*
using (var scope = app.Services.CreateScope())
{
    var simulationService = scope.ServiceProvider.GetRequiredService<SimulationService>();
    var bettingService = scope.ServiceProvider.GetRequiredService<BettingService>();
}
*/
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
