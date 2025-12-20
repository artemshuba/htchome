using System;
using System.Threading.Tasks;

namespace Home.Base.Services
{
    public interface IConfigurationService
    {
        T? GetValue<T>(string key);
        void SetValue<T>(string key, T value);
        Task SaveAsync();
    }
}
