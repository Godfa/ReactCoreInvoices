using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Services;
using Application.Core;
using Application.ExpenseItems;
using Application.Interfaces;
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

                // Use PostgreSQL
                opt.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(60);
                });
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

                    policy.AllowAnyMethod()
                          .AllowAnyHeader()
                          .WithOrigins(allowedOrigins);
                });
            });
            services.AddMediatR(typeof(Application.Invoices.List.Handler).Assembly);
            services.AddIdentityServices(config);
            services.AddAutoMapper(typeof(MappingProfiles).Assembly);
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<ExpenseItemValidator>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddHttpClient<IReceiptScannerService, ReceiptScannerService>();

            return services;
        }

    }
}