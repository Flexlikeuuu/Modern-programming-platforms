using System;

namespace TestingLibrary
{
    public class TestingException : Exception
    {
        public TestingException(string message) : base(message) { }
    }
}