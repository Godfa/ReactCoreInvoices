using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Identity;

namespace Persistence
{
    public class Seed
    {
        public static async Task SeedData(DataContext context, UserManager<User> userManager)
        {

            if (!userManager.Users.Any())
            {
                var users = new List<User>
                {
                    new User{DisplayName= "Epi", UserName = "epi", Email = "epituo@gmail.com"}, 
                    new User{DisplayName= "Epityö", UserName="epityo", Email = "esa.hamola@atrsoft.com"}
                };

                foreach(var user in users)
                {
                    await userManager.CreateAsync(user, "Pa$$w0rd");
                }
            }

            if (context.Invoices.Any()) return;

            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    LanNumber = 68,
                    Description = "Mökkilan 68", 
                    Amount = 415.24m,
                    ExpenseItems = new List<ExpenseItem>
                    {
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Kauppalista" , 
                            ExpenseCreditor = 1                                  
                        }                         

                    }
                }
            };
            await context.Invoices.AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }
    }
}