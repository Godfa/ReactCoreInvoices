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
            var urpiUser = await userManager.FindByEmailAsync("urpi@example.com");
            var zeipUser = await userManager.FindByEmailAsync("zeip@example.com");
            var sakkeUser = await userManager.FindByEmailAsync("sakke@example.com");

            // Mökkilan 80 osallistujat (8 henkilöä)
            var mokkila80Participants = new List<User> { jaapuUser, leivoUser, timoUser, jhattuUser, urpiUser, epiUser, sakkeUser, zeipUser }
                .Where(u => u != null).ToList();

            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    LanNumber = 80,
                    Title = "Mökkilan 80",
                    Description = "Villa Aleksi",
                    Image = "mokkila",
                    Status = InvoiceStatus.Aktiivinen,
                    ExpenseItems = new List<ExpenseItem>
                    {
                        // Mökin ennakkomaksu - 325,00 € - Maksaja: Jani (JHattu) - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Mökin ennakkomaksu",
                            OrganizerId = jhattuUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Mökin ennakkomaksu", Quantity = 1, UnitPrice = 325.00m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Mökin loppulasku - 760,00 € - Maksaja: Jani (JHattu) - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Mökin loppulasku",
                            OrganizerId = jhattuUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Mökin loppulasku", Quantity = 1, UnitPrice = 760.00m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Alko - 117,69 € - Maksaja: Zeip - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Alko",
                            OrganizerId = zeipUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Alkoholijuomat", Quantity = 1, UnitPrice = 117.69m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Ruoat pl. burgerit - 190,79 € - Maksaja: Zeip - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Ruoat pl. burgerit",
                            OrganizerId = zeipUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Ruokaostokset", Quantity = 1, UnitPrice = 190.79m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Lucifer sytytyspalat - 5,90 € - Maksaja: Epi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Lucifer sytytyspalat",
                            OrganizerId = epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Sytytyspalat", Quantity = 1, UnitPrice = 5.90m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Taksi ravintolasta - 64,50 € - Maksaja: Leivo - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Taksi ravintolasta",
                            OrganizerId = leivoUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Taksimatka", Quantity = 1, UnitPrice = 64.50m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Jumperin bensat - 106,95 € - Maksaja: Leivo - Jaetaan vain Jarnolle (Jaapu) ja Leivolle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Gasoline,
                            Name = "Jumperin bensat (610 km)",
                            OrganizerId = leivoUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Bensiini 610 km, 10.6 l/100km, 1.654€/l", Quantity = 1, UnitPrice = 106.95m }
                            },
                            Payers = new List<ExpenseItemPayer>
                            {
                                new ExpenseItemPayer { AppUserId = jaapuUser?.Id ?? epiUser.Id, AppUser = jaapuUser ?? epiUser },
                                new ExpenseItemPayer { AppUserId = leivoUser?.Id ?? epiUser.Id, AppUser = leivoUser ?? epiUser }
                            }
                        },
                        // Taksi ravintolaan - 60,40 € - Maksaja: Urpi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Taksi ravintolaan",
                            OrganizerId = urpiUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Taksimatka", Quantity = 1, UnitPrice = 60.40m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Burgeri / ruoka 1 - 59,46 € - Maksaja: Urpi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Burgeri / ruoka 1",
                            OrganizerId = urpiUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Burgerit", Quantity = 1, UnitPrice = 59.46m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        },
                        // Pyyhkeet, hiilet, jäät - 16,96 € - Maksaja: Urpi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Pyyhkeet, hiilet, jäät",
                            OrganizerId = urpiUser?.Id ?? epiUser.Id,
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Sekalaiset", Quantity = 1, UnitPrice = 16.96m }
                            },
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id, AppUser = u }).ToList()
                        }
                    },
                    Participants = mokkila80Participants.Select(u => new InvoiceParticipant
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