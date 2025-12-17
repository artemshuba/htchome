using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HTCHome.Utils.Helpers
{
    public sealed class JsonStateFileStore : IStateStore
    {
        private readonly JsonSerializerOptions _options;
        private readonly AsyncLock _lock = new();

        public JsonStateFileStore(JsonSerializerOptions? options = null)
        {
            _options = options ?? CreateDefaultOptions();
        }

        public async Task<T?> LoadAsync<T>(string path, CancellationToken ct = default) where T: class, new()
        {
            using (await _lock.LockAsync(ct).ConfigureAwait(false))
            {
                if (!File.Exists(path))
                    return null;

                try
                {
                    await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var value = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
                    return value;
                }
                catch
                {
                    // TODO: log (broken json / IO errors)
                    Debug.WriteLine("Failed to load JSON file: " + path);
                    return null;
                }
            }
        }

        public async Task SaveAsync<T>(T value, string path, CancellationToken ct = default) where T : class, new()
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            using (await _lock.LockAsync(ct).ConfigureAwait(false))
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var tmp = path + ".tmp";
                var bak = path + ".bak";

                // Write temp file
                await using (var stream = File.Open(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await JsonSerializer.SerializeAsync(stream, value, _options, ct).ConfigureAwait(false);
                    await stream.FlushAsync(ct).ConfigureAwait(false);
                }

                // Atomically replace
                try
                {
                    if (File.Exists(path))
                    {
                        // Replace target, keep a backup
                        File.Replace(tmp, path, bak, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(tmp, path);
                    }
                }
                finally
                {
                    // Best-effort cleanup
                    TryDelete(tmp);
                    TryDelete(bak);
                }
            }
        }

        public void Delete(string path)
        {
            TryDelete(path);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                /* ignore */
            }
        }

        private static JsonSerializerOptions CreateDefaultOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
        }

        /// <summary>Simple async mutual exclusion.</summary>
        private sealed class AsyncLock
        {
            private readonly SemaphoreSlim _semaphore = new(1, 1);

            public async Task<IDisposable> LockAsync(CancellationToken ct)
            {
                await _semaphore.WaitAsync(ct).ConfigureAwait(false);
                return new Releaser(_semaphore);
            }

            private sealed class Releaser : IDisposable
            {
                private readonly SemaphoreSlim _s;
                public Releaser(SemaphoreSlim s) => _s = s;
                public void Dispose() => _s.Release();
            }
        }
    }
}