using CinemaLogic;
using System.Threading.Tasks;
using System;
using TestingLibrary;

namespace CinemaTests
{
    [TestClass]
    public class CinemaBusinessTests
    {
        private BillingService _billing;
        private User _testUser;

        private static SharedContext _context = new SharedContext();

        [Before]
        public void Init()
        {
            _billing = new BillingService();
            _testUser = new User { Name = "Alice", Age = 25, Balance = 50, Region = "EU", ConnectedDevices = 1 };
        }

        [TestMethod("Запись данных в Shared Context")]
        public void Step1_WriteToSharedContext()
        {
            _context.Set("GlobalPromo", "SAVE50");
            _context.Set("LastUserRegion", _testUser.Region);

            Assert.IsNotNull(_context.Get<string>("GlobalPromo"));
        }

        [TestMethod("Чтение данных из Shared Context")]
        public void Step2_ReadFromSharedContext()
        {
            string promo = _context.Get<string>("GlobalPromo");
            string region = _context.Get<string>("LastUserRegion");

            Assert.AreEqual("SAVE50", promo);
            Assert.AreEqual("EU", region);

            decimal price = _billing.CalculatePrice(SubscriptionType.Standard, region);
            decimal discounted = _billing.ApplyPromoCode(promo, price);

            Assert.AreEqual(5.5m, discounted);
        }

        [TestMethod("Цена в US")]
        public void TestRegionalPricing()
        {
            decimal price = _billing.CalculatePrice(SubscriptionType.Premium, "US");
            Assert.AreEqual(25m, price);
            Assert.IsInRange((double)price, 20, 30);
        }

        [TestCase(SubscriptionType.Premium, "EU", 22.0)]
        [TestCase(SubscriptionType.Standard, "RU", 10.0)]
        [TestCase(SubscriptionType.Standard, "US", 5.0)]
        [TestCase(SubscriptionType.Free, "RU", 0.0)]
        public void TestPriceCases(SubscriptionType type, string reg, double expected)
        {
            decimal price = _billing.CalculatePrice(type, reg);
            Assert.AreEqual((decimal)expected, price);
        }

        [TestMethod("Проверка ограничений")]
        public void TestRestrictions()
        {
            var movie = new Movie { Title = "R-Rated", MinAge = 18 };
            Assert.IsTrue(_billing.CanWatch(_testUser, movie));

            _testUser.Age = 10;
            Assert.IsFalse(_billing.CanWatch(_testUser, movie));
        }

        [TestMethod("Асинхронная оплата")]
        public async Task TestPaymentAsync()
        {
            bool success = await _billing.ProcessPaymentAsync(_testUser, 10m);
            Assert.IsTrue(success);
        }

        [TestMethod("Проверка исключения")]
        public void TestException()
        {
            Assert.Throws<CinemaException>(() => {
                var t = _billing.ProcessPaymentAsync(_testUser, 999m);
                t.GetAwaiter().GetResult();
            });
        }

        [TestMethod("Проверки типов и строк")]
        public void TestTypesAndStrings()
        {
            Assert.IsNotNull(_testUser);
            Assert.IsInstanceOf<User>(_testUser);
            Assert.StringContains("Bob", _testUser.Name);
        }

        [After]
        public void Cleanup()
        {
            _billing = null;
            _testUser = null;
        }
    }
}