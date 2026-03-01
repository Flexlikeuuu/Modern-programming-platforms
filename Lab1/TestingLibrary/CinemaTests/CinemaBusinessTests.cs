using TestingLibrary;
using CinemaLogic;
using System.Threading.Tasks;
using System;

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

        [TestMethod("Цена в US")]
        public void TestRegionalPricing()
        {
            decimal price = _billing.CalculatePrice(SubscriptionType.Premium, "US");
            Assert.AreEqual(24m, price);
            Assert.IsInRange((double)price, 20, 30);
        }
        [TestCase(SubscriptionType.Premium, "EU", 22.0)]
        [TestCase(SubscriptionType.Standard, "RU", 10.0)]
        [TestCase(SubscriptionType.Free, "RU", 0.0)]
        [TestCase(SubscriptionType.Standard, "US", 999.0)] 
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
        [TestMethod("Тест лимита устройств (Провальный)")]
        public void TestDeviceLimitFail()
        {
            _testUser.ConnectedDevices = 5;
            var movie = new Movie { Title = "Cartoon", MinAge = 0 };
            Assert.IsTrue(_billing.CanWatch(_testUser, movie));
        }

        [TestMethod("Промокоды")]
        public void TestPromos()
        {
            decimal price = _billing.ApplyPromoCode("SAVE50", 100m);
            Assert.AreEqual(50m, price);
            Assert.AreNotEqual(100m, price);
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
            Assert.StringContains("Ali", _testUser.Name);

            object nullObj = null;
            Assert.IsNull(nullObj);
        }

        [TestMethod("Тест на содержание строки")]
        public void TestStringFail()
        {
            Assert.StringContains("Bob", _testUser.Name);
        }
        [TestMethod("Проверка работы Shared Context")]
        public void TestSharedContextUsage()
        {
            _context.Set("SavedPromoCode", "FREE");
            _context.Set("TempDiscount", 50m);

            string code = _context.Get<string>("SavedPromoCode");
            decimal discount = _context.Get<decimal>("TempDiscount");

            string notFound = _context.Get<string>("NonExistentKey");

            Assert.AreEqual("FREE", code);
            Assert.AreEqual(50m, discount);
            Assert.IsNull(notFound);
        }

        [After]
        public void Cleanup()
        {
            _billing = null;
            _testUser = null;
        }
    }
}