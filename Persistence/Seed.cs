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

            // Create users if they don't exist (never overwrite existing user data)
            foreach (var (displayName, userName, email) in expectedUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser == null)
                {
                    // Create new user with minimal data
                    // Note: Sensitive fields (PhoneNumber, BankAccount) are intentionally not set here
                    var newUser = new User
                    {
                        DisplayName = displayName,
                        UserName = userName,
                        Email = email,
                        MustChangePassword = true
                        // PhoneNumber and BankAccount are null by default - users set these in their profile
                    };

                    // Generate random password (length 20) - no need to log as it will be reset via admin
                    var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 20);
                    await userManager.CreateAsync(newUser, randomPassword);
                }
                else
                {
                    // User already exists - preserve all their data including profile information
                    // Never overwrite DisplayName, Email, PhoneNumber, BankAccount, or any other user data
                }
            }


            // Add Epi to Admin role
            var epiUser = await userManager.FindByEmailAsync("epituo@gmail.com");
            User leivoUser, jaapuUser, timoUser, jhattuUser, urpiUser, zeipUser, sakkeUser;
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
                context.ChangeTracker.Clear();
            }

            // Only seed invoices if none exist
            if (context.Invoices.Any()) return;

            // Re-fetch users after potential tracker clearing and ensure they are all tracked in the current state
            epiUser = await userManager.FindByEmailAsync("epituo@gmail.com");
            leivoUser = await userManager.FindByEmailAsync("leivo@example.com");
            jaapuUser = await userManager.FindByEmailAsync("jaapu@example.com");
            timoUser = await userManager.FindByEmailAsync("timo@example.com");
            jhattuUser = await userManager.FindByEmailAsync("jhattu@example.com");
            urpiUser = await userManager.FindByEmailAsync("urpi@example.com");
            zeipUser = await userManager.FindByEmailAsync("zeip@example.com");
            sakkeUser = await userManager.FindByEmailAsync("sakke@example.com");

            if (epiUser == null) return;

            // Mökkilan 80 osallistujat (8 henkilöä)
            var mokkila80Participants = new List<User> { jaapuUser, leivoUser, timoUser, jhattuUser, urpiUser, epiUser, sakkeUser, zeipUser }
                .Where(u => u != null)
                .GroupBy(u => u.Id).Select(g => g.First())
                .ToList();

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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                                new ExpenseItemPayer { AppUserId = jaapuUser?.Id ?? epiUser.Id },
                                new ExpenseItemPayer { AppUserId = leivoUser?.Id ?? epiUser.Id }
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
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
                            Payers = mokkila80Participants.Select(u => new ExpenseItemPayer { AppUserId = u.Id }).ToList()
                        }
                    },
                    Participants = mokkila80Participants.Select(u => new InvoiceParticipant
                    {
                        AppUserId = u.Id
                    }).ToList()
                }
            };
            // Fix: Explicitly set the IDs for the join entities to avoid identity tracking conflicts during AddRangeAsync
            foreach (var invoice in invoices)
            {
                if (invoice.Participants != null)
                {
                    foreach (var participant in invoice.Participants)
                    {
                        participant.InvoiceId = invoice.Id;
                    }
                }

                if (invoice.ExpenseItems != null)
                {
                    foreach (var item in invoice.ExpenseItems)
                    {
                        if (item.Payers != null)
                        {
                            foreach (var payer in item.Payers)
                            {
                                payer.ExpenseItemId = item.Id;
                            }
                        }
                    }
                }
            }

            context.ChangeTracker.Clear();
            /*
            */
            var allPayers = invoices.SelectMany(i => i.ExpenseItems ?? new List<ExpenseItem>())
                                    .SelectMany(ei => ei.Payers ?? new List<ExpenseItemPayer>())
                                    .ToList();

            var duplicates = allPayers.GroupBy(p => new { p.ExpenseItemId, p.AppUserId })
                                     .Where(g => g.Count() > 1)
                                     .ToList();

            if (duplicates.Any())
            {
                foreach (var dup in duplicates)
                {
                    Console.WriteLine($"DUPLICATE Payer Key: ExpenseItem={dup.Key.ExpenseItemId}, User={dup.Key.AppUserId}, Count={dup.Count()}");
                }
            }

            context.ChangeTracker.Clear();
            foreach (var invoice in invoices)
            {
                // Extract items and participants to add them separately
                var items = invoice.ExpenseItems;
                var participants = invoice.Participants;
                invoice.ExpenseItems = null;
                invoice.Participants = null;

                await context.Invoices.AddAsync(invoice);
                await context.SaveChangesAsync();

                if (participants != null)
                {
                    foreach (var participant in participants)
                    {
                        participant.InvoiceId = invoice.Id;
                        await context.InvoiceParticipants.AddAsync(participant);
                    }
                    await context.SaveChangesAsync();
                }

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var payers = item.Payers;
                        item.Payers = null;
                        item.InvoiceId = invoice.Id;

                        await context.ExpenseItems.AddAsync(item);
                        await context.SaveChangesAsync();

                        if (payers != null)
                        {
                            foreach (var payer in payers.Where(p => p.AppUserId != null).GroupBy(p => p.AppUserId).Select(g => g.First()))
                            {
                                payer.ExpenseItemId = item.Id;
                                await context.ExpenseItemPayers.AddAsync(payer);
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }
        }
    }
}