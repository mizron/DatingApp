/*
 * File: ApplicationServicesExtensions
 *
 * Description: Class used to implement extension methods
 *              - Extend the IServiceCollection with application services
 *              
 */
using API.Services;
using API.Interfaces;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Add Token Service to container
            // use AddScoped - scoped for the lifetime of Http request then destroyed
            services.AddScoped<ITokenService, TokenService>();

            // Add connection string to connect to Sqlite db from application 
            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlite(config.GetConnectionString("DefaultConnection"));
          
            });

            return services;
        }
    }
}