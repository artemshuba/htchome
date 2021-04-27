using Microsoft.Extensions.DependencyInjection;
using System;

namespace HTCHome
{
    public static class Ioc
    {
        private static IServiceProvider _serviceProvider;

        static Ioc()
        {
            var serviceCollection = new ServiceCollection();
            SetupServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private static void SetupServices(IServiceCollection services)
        {

        }
    }
}
