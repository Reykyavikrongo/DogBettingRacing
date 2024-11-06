using DogBettingRacing.Data;
using DogBettingRacing.Models;
using Microsoft.EntityFrameworkCore;

namespace DogBettingRacing.Services
{
    public class BetService : BackgroundService
    {
        private readonly Random _random = new Random();
        private readonly ILogger<BetService> _logger;
        private readonly IServiceScopeFactory _ScopeFactory;
        public BetService(ILogger<BetService> logger, IServiceScopeFactory scopeFactory) 
        {
            _logger = logger;
            _ScopeFactory=scopeFactory;
        }

        public async Task<bool> PlaceBetAsync(AppDbContext _context)
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                var dogs = await _context.Dogs.Include(d=>d.Round).Where(d => d.Round.Status == "Scheduled").ToListAsync();

                if (!dogs.Any())
                {
                    _logger.LogWarning("No dogs to place a bet.");
                    return false;
                }
                if (!users.Any())
                {
                    _logger.LogWarning("No users to place a bet.");
                    return false;
                }

                var user = users[_random.Next(users.Count)];
                var dog = dogs[_random.Next(dogs.Count)];

                decimal betAmount = _random.Next(1, 151);

                var bet = new Bet
                {
                    UserId = user.UserId,
                    DogId = dog.DogId,
                    Amount = betAmount,
                    Status = "Pending"
                };

                if (user == null || dog == null || dog.Round.Status != "Scheduled" || user.WalletBalance < betAmount)
                {
                    if (betAmount > 100 || betAmount < 3)
                    {
                        _logger.LogWarning("Bet Amount must be between 3 and 100");
                        bet.Status = "Rejected";
                    }
                    else
                    {
                        _logger.LogWarning("Failed to place bet: invalid conditions met.");
                        bet.Status = "Rejected";
                        return false;
                    }
                }
                else
                {
                    user.WalletBalance -= betAmount;
                    bet.Status = "Pending";

                    _context.Bets.Add(bet);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Bet placed for user {bet.UserId} on dog {bet.DogId} with amount {bet.Amount}");

                    return true;
                }

                await _context.Entry(dog.Round).ReloadAsync();

                if (dog.Round.Status != "Scheduled")
                {
                    user.WalletBalance += betAmount;
                    bet.Status = "Rejected";
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Bet {bet.BetId} was invalidated as round {dog.RoundId} started during processing.");
                    return false;
                }

                bet.Status = "Success";
                await _context.SaveChangesAsync();
                return false;

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
            return false;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                try
                {
                    using (var scope = _ScopeFactory.CreateScope())
                    {
                        var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        await PlaceBetAsync(_context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while placing a random bet.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
