using System.Collections.Concurrent;

namespace TestingLibrary
{
    public class SharedContext
    {
        private readonly ConcurrentDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();
        public void Set(string key, object value) => _storage[key] = value;
        public T Get<T>(string key) => _storage.TryGetValue(key, out var val) ? (T)val : default;
    }
}