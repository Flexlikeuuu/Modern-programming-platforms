using System;

namespace TestingLibrary
{
    public static class Assert
    {
        public static void AreEqual(object exp, object act) => AreEqualInternal(exp, act, true);
        public static void AreNotEqual(object unexp, object act) => AreEqualInternal(unexp, act, false);
        private static void AreEqualInternal(object a, object b, bool eq)
        {
            if (eq && !Equals(a, b)) throw new TestingException($"Expected <{a}>, got <{b}>");
            if (!eq && Equals(a, b)) throw new TestingException($"Value <{b}> should NOT be equal to <{a}>");
        }
        public static void IsTrue(bool c) { if (!c) throw new TestingException("Expected True"); }
        public static void IsFalse(bool c) { if (c) throw new TestingException("Expected False"); }
        public static void IsNull(object o) { if (o != null) throw new TestingException("Object is not Null"); }
        public static void IsNotNull(object o) { if (o == null) throw new TestingException("Object is Null"); }
        public static void StringContains(string s, string f)
        {
            if (f == null || !f.Contains(s)) throw new TestingException($"'{f}' doesn't contain '{s}'");
        }
        public static void IsInRange(double v, double min, double max)
        {
            if (v < min || v > max) throw new TestingException($"{v} out of range [{min}, {max}]");
        }
        public static void IsInstanceOf<T>(object o)
        {
            if (!(o is T)) throw new TestingException($"Object is not {typeof(T).Name}");
        }
        public static void Throws<T>(Action a) where T : Exception
        {
            try { a(); }
            catch (T) { return; }
            catch (Exception ex) { throw new TestingException($"Got {ex.GetType().Name}, expected {typeof(T).Name}"); }
            throw new TestingException($"No exception thrown");
        }
    }
}