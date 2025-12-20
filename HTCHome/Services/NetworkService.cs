using Home.Base.Services;
using System.Net.Http;

namespace HTCHome.Services
{
    public class NetworkService : INetworkService
    {
        // Simple implementation for now. 
        // TODO: Add Proxy support here later.
        
        public NetworkService()
        {
        }

        public HttpClient CreateClient(string? name = null)
        {
            // Simple implementation for now. 
            // TODO: Add Proxy support here later.
            return new HttpClient(); 
        }
    }
}
