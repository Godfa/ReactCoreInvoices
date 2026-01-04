using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Services;
using Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Persistence;

namespace API.Extensions
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services,
         IConfiguration config)
         {
             services.AddIdentity<User, IdentityRole>(opt=>
             {
                 opt.Password.RequireNonAlphanumeric = false;
             })
             .AddEntityFrameworkStores<DataContext>()
             .AddSignInManager<SignInManager<User>>();

             // Configure Identity to not redirect on unauthorized requests (for API)
             services.ConfigureApplicationCookie(opt =>
             {
                 opt.Events.OnRedirectToLogin = context =>
                 {
                     context.Response.StatusCode = 401;
                     return Task.CompletedTask;
                 };
             });

             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"] ?? "Super secret key"));

             services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
             .AddJwtBearer(opt =>
             {
                 opt.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = key,
                     ValidateIssuer = false,
                     ValidateAudience = false
                 };
             });
             services.AddScoped<TokenService>();

             return services;
         } 
    }
}