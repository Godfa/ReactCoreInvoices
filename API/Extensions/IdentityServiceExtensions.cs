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
             services.AddIdentityCore<User>(opt=>
             {
                 opt.Password.RequireNonAlphanumeric = false;
             })
             .AddRoles<IdentityRole>()
             .AddEntityFrameworkStores<DataContext>()
             .AddSignInManager<SignInManager<User>>()
             .AddRoleManager<RoleManager<IdentityRole>>()
             .AddDefaultTokenProviders();

             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"] ?? "Super secret key"));

             services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
             .AddJwtBearer(opt =>
             {
                 opt.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = key,
                     ValidateIssuer = false,
                     ValidateAudience = false,
                     NameClaimType = "unique_name"
                 };
             });
             services.AddScoped<TokenService>();

             return services;
         } 
    }
}