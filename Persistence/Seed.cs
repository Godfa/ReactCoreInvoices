using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
                    new User{DisplayName= "Leivo", UserName = "leivo", Email = "leivo@example.com"},
                    new User{DisplayName= "Jaapu", UserName = "jaapu", Email = "jaapu@example.com"},
                    new User{DisplayName= "Timo", UserName = "timo", Email = "timo@example.com"},
                    new User{DisplayName= "JHattu", UserName = "jhattu", Email = "jhattu@example.com"},
                    new User{DisplayName= "Urpi", UserName = "urpi", Email = "urpi@example.com"},
                    new User{DisplayName= "Zeip", UserName = "zeip", Email = "zeip@example.com"},
                    new User{DisplayName= "Antti", UserName = "antti", Email = "antti@example.com"},
                    new User{DisplayName= "Sakke", UserName = "sakke", Email = "sakke@example.com"},
                    new User{DisplayName= "Lasse", UserName = "lasse", Email = "lasse@example.com"}
                };

                foreach(var user in users)
                {
                    await userManager.CreateAsync(user, "Pa$$w0rd");
                }
            }

            if (!context.Creditors.Any())
            {
                var creditors = new List<Creditor>
                {
                    new Creditor { Name = "Epi", Email = "epi@example.com" },
                    new Creditor { Name = "Leivo", Email = "leivo@example.com" },
                    new Creditor { Name = "Jaapu", Email = "jaapu@example.com" },
                    new Creditor { Name = "Timo", Email = "timo@example.com" },
                    new Creditor { Name = "JHattu", Email = "jhattu@example.com" },
                    new Creditor { Name = "Urpi", Email = "urpi@example.com" },
                    new Creditor { Name = "Zeip", Email = "zeip@example.com" },
                    new Creditor { Name = "Antti", Email = "antti@example.com" },
                    new Creditor { Name = "Sakke", Email = "sakke@example.com" },
                    new Creditor { Name = "Lasse", Email = "lasse@example.com" }
                };
                await context.Creditors.AddRangeAsync(creditors);
                await context.SaveChangesAsync();
            }

            if (context.Invoices.Any()) return;

            // Get the first creditor (Epi) for the expense item
            var firstCreditor = await context.Creditors.FirstOrDefaultAsync();
            if (firstCreditor == null) return;

            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    LanNumber = 68,
                    Title = "Mökkilan 68",
                    Description = "Nousiaisissa",
                    Image = null,
                    ExpenseItems = new List<ExpenseItem>
                    {
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Kauppalista" ,
                            ExpenseCreditor = firstCreditor.Id,
                            Amount = 415.24m
                        }

                    }
                }
            };
            await context.Invoices.AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }
    }
}