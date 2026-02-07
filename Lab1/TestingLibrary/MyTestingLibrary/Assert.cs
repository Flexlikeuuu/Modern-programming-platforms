using System;

namespace TestingLibrary
{
    public static class Assert
    {
        public static void AreEqual(object exp, object act)
        {
            if (!Equals(exp, act)) throw new TestingException($"Expected <{exp}>, got <{act}>");
        }
        public static void AreNotEqual(object unexp, object act)
        {
            if (Equals(unexp, act)) throw new TestingException($"Value <{act}> should not be equal to <{unexp}>");
        }
        public static void IsTrue(bool cond)
        {
            if (!cond) throw new TestingException("Condition is False");
        }
        public static void IsFalse(bool cond)
        {
            if (cond) throw new TestingException("Condition is True");
        }
        public static void IsNull(object obj)
        {
            if (obj != null) throw new TestingException("Object is not Null");
        }
        public static void IsNotNull(object obj)
        {
            if (obj == null) throw new TestingException("Object is Null");
        }
        public static void StringContains(string sub, string full)
        {
            if (full == null || !full.Contains(sub)) throw new TestingException($"String does not contain '{sub}'");
        }
        public static void IsInRange(double val, double min, double max)
        {
            if (val < min || val > max) throw new TestingException($"{val} is out of range [{min}, {max}]");
        }
        public static void IsInstanceOf<T>(object obj)
        {
            if (!(obj is T)) throw new TestingException($"Object is not {typeof(T).Name}");
        }
        public static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            catch (Exception ex) { throw new TestingException($"Expected {typeof(T).Name}, but got {ex.GetType().Name}"); }
            throw new TestingException($"No exception thrown, expected {typeof(T).Name}");
        }
    }
}