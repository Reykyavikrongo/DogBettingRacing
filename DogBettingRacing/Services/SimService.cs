using DogBettingRacing.Data;
using DogBettingRacing.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

namespace DogBettingRacing.Services
{
    public class SimService : BackgroundService
    {
        private readonly TimeSpan TickInterval;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimService> _logger;
        private readonly HttpClient _httpClient;
        private int n_ofScheduledRounds;
        public SimService(IServiceScopeFactory scopeFactory, ILogger<SimService> logger, HttpClient httpClient)
        {
            Console.WriteLine("SimService constructor reached.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeFactory=scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _httpClient=httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            TickInterval = TimeSpan.FromSeconds(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    _logger.LogInformation("(LOG) Inside scope running");
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await EnsureScheduledRounds(context);

                    await GenerateOddsAndResults(context);

                    await context.SaveChangesAsync(stoppingToken);
                }

                _logger.LogInformation("SimService is running.");
                await Task.Delay(TickInterval, stoppingToken);
            }
        }

        private async Task EnsureScheduledRounds(AppDbContext context)
        {
            _logger.LogInformation("EnsureScheduledRounds");
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                n_ofScheduledRounds = await context.Rounds
                    .CountAsync(r => r.Status == "Scheduled");

                for (int i = n_ofScheduledRounds; i < 5; i++)
                {
                    var availableDogs = await context.Dogs
                        .Include(d => d.Round)
                        .Where(d => d.Round.Status == "Ended")
                        .Take(5)
                        .ToListAsync();

                    if (availableDogs.Count < 5)
                    {
                        _logger.LogWarning("Not enough available dogs to create a new round. Required: 5, Found: {0}", availableDogs.Count);
                    }

                    var newRound = new Round
                    {
                        StartTime = DateTime.UtcNow.AddMinutes(i + 5),
                        Status = "Scheduled"
                    };
                    context.Rounds.Add(newRound);
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Created round {newRound.RoundId}, starts at {newRound.StartTime}");

                    foreach (var dog in availableDogs)
                    {
                        dog.RoundId = newRound.RoundId;
                    }

                    await context.SaveChangesAsync();

                    _logger.LogInformation("New round created with 5 dogs assigned to it.");
                }
                await transaction.CommitAsync();

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
            finally
            {
                if (transaction.GetDbTransaction().Connection != null)
                {
                    await transaction.RollbackAsync();
                }
            }
        }

        private async Task GenerateOddsAndResults(AppDbContext context)
        {
            _logger.LogInformation("GenerateOddsAndResults called");
            try
            {
                var roundsStartingSoon = await context.Rounds
                    .Include(r => r.Dogs)
                    .Where(r => r.Status == "Scheduled" && r.StartTime <= DateTime.UtcNow.AddSeconds(5))
                    .ToListAsync();

                foreach (var round in roundsStartingSoon)
                {
                    foreach (var dog in round.Dogs)
                    {
                        dog.Odds = GenerateRandomOdds();
                    }

                    var winningDog = round.Dogs.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                    if (winningDog != null)
                    {
                        round.WinningDogId = winningDog.DogId;
                        _logger.LogInformation($"Dog: {winningDog.DogId} is the winner of round {round.RoundId}");
                    }

                    round.Status = "Ended";
                    n_ofScheduledRounds -= 1;

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
            Random rand = new Random();
            return Math.Round((decimal)(rand.NextDouble() * (5.0 - 1.1) + 1.1), 2);
        }

        private async Task ProcessBets(AppDbContext context, Round round)
        {
            _logger.LogInformation("ProcessBets called");
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
                    _logger.LogInformation($"bet {bet.BetId} Lost.");
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
                    _logger.LogInformation("Simulated data sent successfully.");
                }
                else
                {
                    _logger.LogInformation("Simulated data failed to send.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogInformation($"Simulated request failed: {ex.Message}");
            }
        }
    }
}
