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
            var leivoUser = await userManager.FindByEmailAsync("leivo@example.com");
            var jaapuUser = await userManager.FindByEmailAsync("jaapu@example.com");
            var timoUser = await userManager.FindByEmailAsync("timo@example.com");
            var jhattuUser = await userManager.FindByEmailAsync("jhattu@example.com");

            // Get subset of users for payers
            var usualSuspects = new List<string> { "Epi", "JHattu", "Leivo", "Timo", "Jaapu", "Urpi", "Zeip" };
            var usualSuspectsUsers = allUsers.Where(u => usualSuspects.Contains(u.DisplayName)).ToList();

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
                                    Name = "Ruokaostokset",
                                    Quantity = 1,
                                    UnitPrice = 234.50m
                                },
                                new ExpenseLineItem
                                {
                                    Name = "Juomat",
                                    Quantity = 1,
                                    UnitPrice = 87.30m
                                },
                                new ExpenseLineItem
                                {
                                    Name = "Grillitarvikkeet",
                                    Quantity = 1,
                                    UnitPrice = 45.60m
                                }
                            },
                            Payers = usualSuspectsUsers.Select(u => new ExpenseItemPayer
                            {
                                AppUserId = u.Id,
                                AppUser = u
                            }).ToList()
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Puulaaki",
                            OrganizerId = leivoUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Koivuhalot 2m³",
                                    Quantity = 2,
                                    UnitPrice = 85.00m
                                }
                            },
                            Payers = usualSuspectsUsers.Select(u => new ExpenseItemPayer
                            {
                                AppUserId = u.Id,
                                AppUser = u
                            }).ToList()
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Gasoline,
                            Name = "Polttoaine",
                            OrganizerId = jaapuUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Bensiini 95E10",
                                    Quantity = 50,
                                    UnitPrice = 1.85m
                                }
                            },
                            Payers = usualSuspectsUsers.Select(u => new ExpenseItemPayer
                            {
                                AppUserId = u.Id,
                                AppUser = u
                            }).ToList()
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Saunatarvikkeet",
                            OrganizerId = timoUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Vihdat ja tuoksut",
                                    Quantity = 1,
                                    UnitPrice = 34.90m
                                }
                            },
                            Payers = usualSuspectsUsers.Select(u => new ExpenseItemPayer
                            {
                                AppUserId = u.Id,
                                AppUser = u
                            }).ToList()
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Kalusteet",
                            OrganizerId = jhattuUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Grillihiiliä",
                                    Quantity = 3,
                                    UnitPrice = 8.50m
                                },
                                new ExpenseLineItem
                                {
                                    Name = "Kertakäyttöastiat",
                                    Quantity = 1,
                                    UnitPrice = 15.90m
                                }
                            },
                            Payers = usualSuspectsUsers.Select(u => new ExpenseItemPayer
                            {
                                AppUserId = u.Id,
                                AppUser = u
                            }).ToList()
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Sähkö",
                            OrganizerId = epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Sähkölasku",
                                    Quantity = 1,
                                    UnitPrice = 67.80m
                                }
                            },
                            Payers = new List<ExpenseItemPayer>
                            {
                                new ExpenseItemPayer { AppUserId = epiUser.Id, AppUser = epiUser }
                            }
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Kalustevuokra",
                            OrganizerId = leivoUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Telttavuokra viikonlopuksi",
                                    Quantity = 1,
                                    UnitPrice = 120.00m
                                }
                            },
                            Payers = new List<ExpenseItemPayer>
                            {
                                new ExpenseItemPayer { AppUserId = leivoUser?.Id ?? epiUser.Id, AppUser = leivoUser ?? epiUser },
                                new ExpenseItemPayer { AppUserId = jaapuUser?.Id ?? epiUser.Id, AppUser = jaapuUser ?? epiUser }
                            }
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Tupakka",
                            OrganizerId = timoUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Marlboro Red",
                                    Quantity = 2,
                                    UnitPrice = 9.50m
                                }
                            },
                            Payers = new List<ExpenseItemPayer>
                            {
                                new ExpenseItemPayer { AppUserId = timoUser?.Id ?? epiUser.Id, AppUser = timoUser ?? epiUser },
                                new ExpenseItemPayer { AppUserId = jhattuUser?.Id ?? epiUser.Id, AppUser = jhattuUser ?? epiUser }
                            }
                        },
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Kalastuslupia",
                            OrganizerId = jaapuUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem
                                {
                                    Name = "Viehelupa",
                                    Quantity = 1,
                                    UnitPrice = 45.00m
                                }
                            },
                            Payers = new List<ExpenseItemPayer>
                            {
                                new ExpenseItemPayer { AppUserId = jaapuUser?.Id ?? epiUser.Id, AppUser = jaapuUser ?? epiUser }
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