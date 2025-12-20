using Home.Base.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HTCHome.Services
{
    public class JsonConfigurationService : IConfigurationService
    {
        private readonly string _filePath;
        private Dictionary<string, JsonElement> _data;
        private Dictionary<string, object> _modifiedData = new();
        private bool _isLoaded = false;

        public JsonConfigurationService(string filePath)
        {
            _filePath = filePath;
            _data = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        public async Task LoadAsync()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    using var stream = File.OpenRead(_filePath);
                    _data = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(stream) 
                            ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                }
                catch
                {
                    _data = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                }
            }
            _isLoaded = true;
        }

        public T? GetValue<T>(string key)
        {
            if (!_isLoaded)
            {
                // Warn: Accessing before load? Or just do sync load if needed?
                // For now assuming initialized.
            }

            if (_modifiedData.TryGetValue(key, out var val))
            {
                if (val is T tVal) return tVal;
                // Try convert?
                return (T)Convert.ChangeType(val, typeof(T));
            }

            if (_data.TryGetValue(key, out var jsonElement))
            {
                try
                {
                    return jsonElement.Deserialize<T>();
                }
                catch
                {
                    return default;
                }
            }

            return default;
        }

        public void SetValue<T>(string key, T value)
        {
            if (value != null)
            {
                _modifiedData[key] = value;
            }
            else
            {
                _modifiedData.Remove(key);
                if (_data.ContainsKey(key))
                {
                    _data.Remove(key); // Also remove from base data to mark as deleted? 
                    // Actually complex to handle deletes with mixed dictionaries.
                    // Simplified: We'll just serialize everything on Save.
                }
            }
        }

        public async Task SaveAsync()
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Merge modified into data (re-serializing)
            // Or simpler: Create a new dictionary for save
            var exportData = new Dictionary<string, object>(_data.Count + _modifiedData.Count);
            
            foreach(var kvp in _data)
            {
                exportData[kvp.Key] = kvp.Value;
            }
            
            foreach(var kvp in _modifiedData)
            {
                exportData[kvp.Key] = kvp.Value;
            }

            using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, exportData, new JsonSerializerOptions { WriteIndented = true });
            
            // Reload to refresh _data with JsonElements
            // Or just await LoadAsync();
        }
    }
}
