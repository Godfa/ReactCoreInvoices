using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Core;
using Application.ExpenseItems;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Persistence;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,
            IConfiguration config)
        {
             services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebAPIv5", Version = "v1" });
            });
            services.AddDbContext<DataContext>(opt =>
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                // Use SQL Server in production (if connection string contains "Server=")
                // Use SQLite in development
                if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
                {
                    opt.UseSqlServer(connectionString);
                }
                else
                {
                    opt.UseSqlite(connectionString);
                }
            });
            services.AddCors(opt =>
            {
                opt.AddPolicy("CorsPolicy", policy =>
                {
                    var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>();

                    // Fallback to environment variable if config section is empty
                    if (allowedOrigins == null || allowedOrigins.Length == 0)
                    {
                        var envOrigin = config["ALLOWED_ORIGINS"];
                        allowedOrigins = !string.IsNullOrEmpty(envOrigin)
                            ? envOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            : new[] { "http://localhost:3000" };
                    }

                    Console.WriteLine($"[CORS DEBUG] Configured origins: {string.Join(", ", allowedOrigins)}");

                    // TEMPORARY: Using AllowAnyOrigin to debug CORS issue
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();

                    // TODO: Revert to specific origins once CORS headers issue is resolved
                    // policy.WithOrigins(allowedOrigins)
                    //       .AllowAnyMethod()
                    //       .AllowAnyHeader();
                });
            });
            services.AddMediatR(typeof(Application.Invoices.List.Handler).Assembly);
            services.AddIdentityServices(config);
            services.AddAutoMapper(typeof(MappingProfiles).Assembly);
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<ExpenseItemValidator>();

            return services;
        }

    }
}