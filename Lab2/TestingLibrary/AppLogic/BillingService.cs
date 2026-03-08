using System;
using System.Threading.Tasks;

namespace CinemaLogic
{
    public class CinemaException : Exception { public CinemaException(string m) : base(m) { } }

    public class BillingService
    {
        public async Task<decimal> CalculatePriceAsync(SubscriptionType type, string region)
        {
            await Task.Delay(200); 
            decimal basePrice = type switch
            {
                SubscriptionType.Standard => 10,
                SubscriptionType.Premium => 20,
                _ => 0
            };
            decimal multiplier = region switch { "US" => 1.2m, "EU" => 1.1m, _ => 1.0m };
            return basePrice * multiplier;
        }

        public async Task<bool> CanWatchAsync(User user, Movie movie)
        {
            await Task.Delay(200);
            if (movie == null) return false;
            if (user.Age < movie.MinAge) return false;
            if (movie.IsPremiumOnly && user.Subscription != SubscriptionType.Premium) return false;
            return true;
        }

        public decimal ApplyPromoCode(string code, decimal currentPrice)
        {
            if (code == "SAVE50") return currentPrice * 0.5m;
            if (code == "FREE") return 0;
            return currentPrice;
        }
    }
}