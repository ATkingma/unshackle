using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger
{
    public class AccountProfile : Profile
    {
    }

    public static class Startup
    {
        public static IServiceCollection AddMapping(this IServiceCollection services) =>
            services.AddSingleton<IConfigurationProvider>(_ =>
            {
                var config = new MapperConfiguration(options =>
                {
                    options.AllowNullCollections = true;
                    options.AddProfile<AccountProfile>();
                });
                return config;
            });
    }
}
