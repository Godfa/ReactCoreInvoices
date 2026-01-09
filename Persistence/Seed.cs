using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Persistence
{
    public class Seed
    {
        public static async Task SeedData(DataContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IHostEnvironment environment)
        {
            // Create roles if they don't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Define expected users
            var expectedUsers = new List<(string DisplayName, string UserName, string Email)>
            {
                ("Epi", "epi", "epituo@gmail.com"),
                ("Leivo", "leivo", "leivo@example.com"),
                ("Jaapu", "jaapu", "jaapu@example.com"),
                ("Timo", "timo", "timo@example.com"),
                ("JHattu", "jhattu", "jhattu@example.com"),
                ("Urpi", "urpi", "urpi@example.com"),
                ("Zeip", "zeip", "zeip@example.com"),
                ("Antti", "antti", "antti@example.com"),
                ("Sakke", "sakke", "sakke@example.com"),
                ("Lasse", "lasse", "lasse@example.com")
            };

            // Create or update users
            foreach (var (displayName, userName, email) in expectedUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser == null)
                {
                    // Create new user
                    var newUser = new User
                    {
                        DisplayName = displayName,
                        UserName = userName,
                        Email = email,
                        MustChangePassword = true
                    };
                    await userManager.CreateAsync(newUser, "Pa$$w0rd");
                }
                else
                {
                    // User already exists - don't reset their password change flag
                }
            }

            // Add Epi to Admin role
            var epiUser = await userManager.FindByEmailAsync("epituo@gmail.com");
            if (epiUser != null)
            {
                var isInRole = await userManager.IsInRoleAsync(epiUser, "Admin");
                if (!isInRole)
                {
                    await userManager.AddToRoleAsync(epiUser, "Admin");
                }
            }

            // Seeding Creditors is removed. Creditors are now Users.

            // Clear existing invoices only in Development environment
            if (environment.IsDevelopment() && context.Invoices.Any())
            {
                var existingInvoices = await context.Invoices
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Payers)
                    .Include(i => i.Participants)
                    .ToListAsync();

                context.Invoices.RemoveRange(existingInvoices);
                await context.SaveChangesAsync();
            }

            // Only seed invoices if none exist
            if (context.Invoices.Any()) return;

            // Get users for participation
            // epiUser is already defined above
            if (epiUser == null) return;
            
            var allUsers = await userManager.Users.ToListAsync();
            
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
                            OrganizerId = epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Imported from original amount",
                                    Quantity = 1,
                                    UnitPrice = 415.24m
                                }
                            }
                        }

                    },
                    Participants = allUsers.Select(u => new InvoiceParticipant
                    {
                        AppUserId = u.Id,
                        AppUser = u
                    }).ToList()
                }
            };
            await context.Invoices.AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }
    }
}