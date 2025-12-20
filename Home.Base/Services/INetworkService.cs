using System;
using System.Net.Http;

namespace Home.Base.Services
{
    public interface INetworkService
    {
        HttpClient CreateClient(string? name = null);
    }
}
