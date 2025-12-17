using System.Threading;
using System.Threading.Tasks;

namespace HTCHome.Utils.Helpers
{
    public interface IStateStore
    {
        Task<T?> LoadAsync<T>(string path, CancellationToken ct = default) where T : class, new();

        Task SaveAsync<T>(T value, string path, CancellationToken ct = default) where T : class, new();

        void Delete(string path);
    }
}