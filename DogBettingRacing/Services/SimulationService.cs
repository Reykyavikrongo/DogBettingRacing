using DogBettingRacing.Data;
using DogBettingRacing.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DogBettingRacing.Services
{
    public class SimulationService : BackgroundService
    {
        private readonly TimeSpan TickInterval;
        private readonly ILogger<SimulationService> _logger;
        private readonly HttpClient _httpClient;
        private int n_activeRounds;
        private readonly IServiceScopeFactory _scopeFactory;

        public SimulationService(ILogger<SimulationService> logger, HttpClient httpClient, IServiceScopeFactory scopeFactory)
        {
            Console.WriteLine("test");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient=httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _scopeFactory=scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            TickInterval = TimeSpan.FromSeconds(10);
        }

        private void StartNewRound()
        {
            var round = new Round { StartTime = DateTime.Now.AddMinutes(1), Status = "Scheduled" };
            n_activeRounds += 1;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("start ExecuteAsync SimulationService");
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await EnsureScheduledRounds(context);

                    await GenerateOddsAndResults(context);

                    await context.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(TickInterval, stoppingToken);
            }
        }
        private async Task EnsureScheduledRounds(AppDbContext context)
        {
            Console.WriteLine("EnsureScheduledRounds");
            try
            {
                int n_ofScheduledRounds = await context.Rounds
                    .CountAsync(r => r.Status == "Scheduled");

                for (int i = n_ofScheduledRounds; i < 5; i++)
                {
                    var newRound = new Round
                    {
                        StartTime = DateTime.UtcNow.AddMinutes(i * 5),
                        Status = "Scheduled"
                    };
                    context.Rounds.Add(newRound);
                    _logger.LogInformation($"Created round {newRound.RoundId}, starts at {newRound.StartTime}");
                }

                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning("Concurrency exception thrown: " + ex.Message);

            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error thrown: " + ex.Message);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error thrown: " + ex.Message);
            }
        }

        private async Task GenerateOddsAndResults(AppDbContext context)
        {
            try
            {
                var roundsStartingSoon = await context.Rounds
                    .Include(r => r.Dogs)
                    .Where(r => r.Status == "Scheduled" && r.StartTime <= DateTime.UtcNow.AddSeconds(5))
                    .ToListAsync();

                foreach (var round in roundsStartingSoon)
                {
                    // Generate random odds for each dog
                    foreach (var dog in round.Dogs)
                    {
                        dog.Odds = GenerateRandomOdds();
                    }

                    // Determine race results by selecting a winner
                    var winningDog = round.Dogs.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                    if (winningDog != null)
                    {
                        round.WinningDogId = winningDog.DogId;
                        _logger.LogInformation($"Dog: {winningDog.DogId} is the winner of round {round.RoundId}");
                    }

                    round.Status = "Ended";
                    n_activeRounds -= 1;

                    // Process bets and calculate winnings
                    await ProcessBets(context, round);
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning("Concurrency exception thrown: " + ex.Message);

            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error thrown: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error thrown: " + ex.Message);
            }
        }

        private decimal GenerateRandomOdds()
        {
            // Generate random odds between 1.1 and 5.0
            Random rand = new Random();
            return Math.Round((decimal)(rand.NextDouble() * (5.0 - 1.1) + 1.1), 2);
        }

        private async Task ProcessBets(AppDbContext context, Round round)
        {
            try
            {
                var winningBets = await context.Bets
                .Include(b => b.Dog)
                .Where(b => b.Dog.RoundId == round.RoundId && b.DogId == round.WinningDogId && b.Status == "Pending")
                .ToListAsync();


                foreach (var bet in winningBets)
                {
                    var dog = round.Dogs.FirstOrDefault(d => d.DogId == bet.DogId);
                    if (dog != null)
                    {
                        // Calculate winnings based on odds
                        bet.Payout = bet.Amount * dog.Odds;
                        bet.Status = "Won";
                        var user = await context.Users.FindAsync(bet.UserId);
                        if (user != null)
                        {
                            user.WalletBalance += bet.Payout;
                            _logger.LogInformation($"user {user.UserId} has won {bet.Payout}");
                        }
                    }
                }

                var losingBets = await context.Bets
                    .Include(b => b.Dog)
                    .Where(b => b.Dog.RoundId == round.RoundId && b.Status == "Pending" && b.DogId != round.WinningDogId)
                    .ToListAsync();

                foreach (var bet in losingBets)
                {
                    bet.Status = "Lost";
                }
                await context.SaveChangesAsync();


                await SendData(winningBets.Concat(losingBets));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning("Concurrency exception thrown: " + ex.Message);

            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error thrown: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error thrown: " + ex.Message);
            }
        }
        private async Task SendData(object data)
        {
            var jsonContent = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var URL = "https://localhost/website"; 

            try
            {
                var response = await _httpClient.PostAsync(URL, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Simulated data sent successfully.");
                }
                else
                {
                    Console.WriteLine("Simulated data failed to send.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Simulated request failed: {ex.Message}");
            }
        }
    }
}
