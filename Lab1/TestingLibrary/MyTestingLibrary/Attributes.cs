using System;

namespace TestingLibrary
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string Description { get; }
        public TestMethodAttribute(string description = "") => Description = description;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Params { get; }
        public TestCaseAttribute(params object[] parameters) => Params = parameters;
    }

    [AttributeUsage(AttributeTargets.Method)] public class BeforeAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class AfterAttribute : Attribute { }
}