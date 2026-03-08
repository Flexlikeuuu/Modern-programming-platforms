using System;

namespace TestingLibrary
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute { public int MaxDegreeOfParallelism { get; set; } = 4; }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string Description { get; }
        public TestMethodAttribute(string d = "") => Description = d;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Params { get; }
        public TestCaseAttribute(params object[] p) => Params = p;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TimeoutAttribute : Attribute
    {
        public int Milliseconds { get; }
        public TimeoutAttribute(int ms) => Milliseconds = ms;
    }

    [AttributeUsage(AttributeTargets.Method)] public class BeforeAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class AfterAttribute : Attribute { }
}