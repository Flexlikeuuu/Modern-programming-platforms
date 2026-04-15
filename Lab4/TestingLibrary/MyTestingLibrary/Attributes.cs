using System;

namespace TestingLibrary
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute { public int MaxDegreeOfParallelism { get; set; } = 4; }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Params { get; }
        public TestCaseAttribute(params object[] p) => Params = p;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MemberDataAttribute : Attribute
    {
        public string MethodName { get; }
        public MemberDataAttribute(string name) => MethodName = name;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CategoryAttribute : Attribute
    {
        public string Name { get; }
        public CategoryAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PriorityAttribute : Attribute
    {
        public int Level { get; }
        public PriorityAttribute(int level) => Level = level;
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