namespace CinemaLogic
{
    public enum SubscriptionType { Free, Standard, Premium }

    public class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public SubscriptionType Subscription { get; set; }
        public decimal Balance { get; set; }
        public string Region { get; set; }
        public int ConnectedDevices { get; set; } 
    }

    public class Movie
    {
        public string Title { get; set; }
        public int MinAge { get; set; }
        public bool IsPremiumOnly { get; set; }
    }
}