using TestingLibrary;
using CinemaLogic;
using System;
using System.Threading.Tasks;

namespace CinemaTests
{
    [TestClass(MaxDegreeOfParallelism = 8)]
    public class CinemaBusinessTests
    {
        private BillingService _billing;
        private User _user;

        [Before]
        public void Init()
        {
            _billing = new BillingService();
            _user = new User { Name = "Alice", Age = 25, Subscription = SubscriptionType.Premium, Balance = 50m, Region = "EU" };
        }

        [TestCase(SubscriptionType.Premium, "US", 24.0)]
        [TestCase(SubscriptionType.Standard, "EU", 11.0)]
        [TestCase(SubscriptionType.Free, "RU", 99.0)] 
        public async Task TestPrices(SubscriptionType t, string r, double exp)
        {
            decimal price = await _billing.CalculatePriceAsync(t, r);
            Assert.AreEqual((decimal)exp, price);
        }

        [TestCase(18, false, true)]  
        [TestCase(30, true, false)]  
        [TestCase(0, false, null)]  
        public async Task TestAccess(int age, bool premium, bool? expected)
        {
            Movie m = expected == null ? null : new Movie { MinAge = age, IsPremiumOnly = premium };
            bool result = await _billing.CanWatchAsync(_user, m);
            if (expected == true) Assert.IsTrue(result);
            else if (expected == false) Assert.IsFalse(result);
            else Assert.IsNull(m);
        }

        [TestCase("SAVE50", 100.0, 50.0)]
        [TestCase("WRONG", 100.0, 100.0)]
        [TestCase("FREE", 100.0, 100.0)] 
        public void TestPromos(string code, double cur, double exp)
        {
            decimal res = _billing.ApplyPromoCode(code, (decimal)cur);
            if (code == "SAVE50") Assert.AreNotEqual((decimal)cur, res);
            Assert.AreEqual((decimal)exp, res);
        }

        [TestMethod] public void TestString() => Assert.StringContains("Ali", _user.Name);

        [TestMethod] public void TestRange() => Assert.IsInRange((double)_user.Balance, 10, 100);

        [TestMethod] public void TestType() => Assert.IsInstanceOf<User>(_user);

        [TestMethod] public void TestNotNull() => Assert.IsNotNull(_billing);

        [TestMethod] public void TestException() => Assert.Throws<CinemaException>(() => throw new CinemaException("Error"));

        [TestMethod]
        [Timeout(1000)]
        public async Task TestTimeout() => await Task.Delay(200);

        [After] public void End() { _billing = null; }
    }
}