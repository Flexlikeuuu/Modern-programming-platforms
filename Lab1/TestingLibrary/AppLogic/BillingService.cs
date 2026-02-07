using System;
using System.Threading.Tasks;

namespace CinemaLogic
{
    public class CinemaException : Exception
    {
        public CinemaException(string m) : base(m) { }
    }

    public class BillingService
    {
        public decimal CalculatePrice(SubscriptionType type, string region)
        {
            decimal basePrice = type switch
            {
                SubscriptionType.Standard => 10,
                SubscriptionType.Premium => 20,
                _ => 0
            };
            decimal multiplier = region switch { "US" => 1.2m, "EU" => 1.1m, _ => 1.0m };
            return basePrice * multiplier;
        }

        public bool CanWatch(User user, Movie movie)
        {
            if (user.Age < movie.MinAge) return false;
            if (movie.IsPremiumOnly && user.Subscription != SubscriptionType.Premium) return false;
            if (user.ConnectedDevices > 3) return false; 
            return true;
        }

        public decimal ApplyPromoCode(string code, decimal currentPrice)
        {
            if (code == "SAVE50") return currentPrice * 0.5m;
            if (code == "FREE") return 0;
            return currentPrice;
        }

        public async Task<bool> ProcessPaymentAsync(User user, decimal amount)
        {
            await Task.Delay(50);
            if (user.Balance < amount) throw new CinemaException("Insufficient funds");
            user.Balance -= amount;
            return true;
        }
    }
}