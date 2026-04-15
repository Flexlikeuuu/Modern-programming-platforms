using TestingLibrary;
using CinemaLogic;
using System;
using System.Collections.Generic;
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
            _user = new User { Name = "Alice", Age = 25, Subscription = SubscriptionType.Standard, Region = "EU" };
        }

        public static IEnumerable<object[]> BulkPriceData()
        {
            yield return new object[] { SubscriptionType.Standard, "US", 12.0m }; 
            yield return new object[] { SubscriptionType.Standard, "EU", 11.0m }; 

            yield return new object[] { SubscriptionType.Premium, "UK", 99.0m };

            yield return new object[] { SubscriptionType.Free, "RU", 0.0m };
            yield return new object[] { SubscriptionType.Standard, "RU", 10.0m };
            yield return new object[] { SubscriptionType.Premium, "RU", 20.0m };
            yield return new object[] { SubscriptionType.Free, "US", 0.0m };
            yield return new object[] { SubscriptionType.Standard, "UK", 10.0m };
            yield return new object[] { SubscriptionType.Premium, "UK", 20.0m };
        }

        [TestMethod]
        [MemberData(nameof(BulkPriceData))]
        [Category("Billing")]
        [Priority(1)]
        public async Task TestPricesBulk(SubscriptionType t, string r, decimal exp)
        {
            decimal res = await _billing.CalculatePriceAsync(t, r);
            Assert.AreEqual(exp, res);
        }

        [TestCase("SAVE50", 100.0, 50.0)] 
        [TestCase("SAVE50", 200.0, 100.0)] 

        [TestCase("SAVE50", 10.0, 1.0)]

        [TestCase("FREE", 100.0, 0.0)]
        [TestCase("WRONG", 100.0, 100.0)]

        [TestCase("WRONG", 100.0, 0.0)]

        [Category("Promos")]
        [Priority(2)]
        public void TestPromos(string code, double cur, double exp)
        {
            decimal res = _billing.ApplyPromoCode(code, (decimal)cur);
            Assert.AreEqual((decimal)exp, res);
        }

        [TestCase(18, false, true)] 
        [TestCase(30, true, false)] 


        [TestCase(18, false, true)]

        [TestCase(6, true, false)]
        [Category("Security")]
        [Priority(1)]
        public async Task TestAccess(int age, bool premium, bool expected)
        {
            _user.Age = 10;
            Movie m = new Movie { MinAge = age, IsPremiumOnly = premium };
            bool result = await _billing.CanWatchAsync(_user, m);
            Assert.AreEqual(expected, result);
        }

        [TestMethod][Category("Smoke")] public void TestNotNull() => Assert.IsNotNull(_billing);

        [After] public void End() { _billing = null; }
    }
}