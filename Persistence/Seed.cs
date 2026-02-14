using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Persistence
{
    public class Seed
    {
        public static async Task SeedData(DataContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IHostEnvironment environment, ILogger logger)
        {
            // Create roles if they don't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Define expected users with stable GUIDs as the canonical identity key.
            // The GUID never changes regardless of email, displayname, or username updates.
            var expectedUsers = new List<(string Id, string DisplayName, string UserName, string Email)>
            {
                ("354c7990-0574-4da4-8659-58b28113eb79", "Epi",    "epi",    "epituo@gmail.com"),
                ("c5a33cd2-85bc-4687-9cdb-9b9f38512e0f", "Leivo",  "leivo",  "leivo@example.com"),
                ("bfef42ed-04b5-4639-aa2a-788784ccad02", "Jaapu",  "jaapu",  "jaapu@example.com"),
                ("9f8e7d6c-5b4a-3f2e-1d0c-9b8a7f6e5d4c", "Timo",   "timo",   "timo@example.com"),
                ("4b5c6d7e-8f9a-0b1c-2d3e-4f5a6b7c8d9e", "JHattu", "jhattu", "jhattu@example.com"),
                ("1c2d3e4f-5a6b-7c8d-9e0f-1a2b3c4d5e6f", "Urpi",   "urpi",   "urpi@example.com"),
                ("7d8e9f0a-1b2c-3d4e-5f6a-7b8c9d0e1f2a", "Zeip",   "zeip",   "zeip@example.com"),
                ("5e6f7a8b-9c0d-1e2f-3a4b-5c6d7e8f9a0b", "Antti",  "antti",  "antti@example.com"),
                ("6f7a8b9c-0d1e-2f3a-4b5c-6d7e8f9a0b1c", "Sakke",  "sakke",  "sakke@example.com"),
                ("0a1b2c3d-4e5f-6a7b-8c9d-0e1f2a3b4c5d", "Lasse",  "lasse",  "lasse@example.com")
            };

            // Create users if they don't exist (never overwrite existing user data).
            // Lookup by stable GUID — the canonical identity for each user.
            foreach (var (id, displayName, userName, email) in expectedUsers)
            {
                var existingUser = await userManager.FindByIdAsync(id);
                if (existingUser == null)
                {
                    // Create new user with the stable seed GUID as the primary key.
                    // Note: Sensitive fields (PhoneNumber, BankAccount) are intentionally not set here.
                    var newUser = new User
                    {
                        Id = id,
                        DisplayName = displayName,
                        UserName = userName,
                        Email = email,
                        MustChangePassword = true,
                        LockoutEnabled = true
                        // PhoneNumber and BankAccount are null by default - users set these in their profile
                    };

                    // Generate random password (length 20) with mix of chars to satisfy default Identity requirements
                    var randomPassword = string.Concat(Guid.NewGuid().ToString("N").AsSpan(0, 10), Guid.NewGuid().ToString("N")[..5].ToUpper(), "!1a");
                    var result = await userManager.CreateAsync(newUser, randomPassword);
                    if (!result.Succeeded)
                    {
                        logger.LogError("Failed to create user {UserName}: {Errors}", userName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                // User already exists - preserve all their data including profile information.
                // Never overwrite DisplayName, Email, PhoneNumber, BankAccount, or any other user data.
            }


            // Add Epi to Admin role
            var epiUser = await userManager.FindByIdAsync("354c7990-0574-4da4-8659-58b28113eb79");
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

            // Resolve actual IDs from the database. Handles both fresh installs and existing
            // databases where users may have been created with different GUIDs previously.
            var userIds = new Dictionary<string, string>();
            foreach (var (seedId, _, userName, _) in expectedUsers)
            {
                var user = await userManager.FindByNameAsync(userName);
                userIds[userName] = user?.Id ?? seedId;
            }

            // Only seed invoices if none exist
            if (context.Invoices.Any()) return;

            // Convenience variables for readability in the invoice initializer below
            var idJHattu = userIds["jhattu"];
            var idZeip   = userIds["zeip"];
            var idEpi    = userIds["epi"];
            var idLeivo  = userIds["leivo"];
            var idUrpi   = userIds["urpi"];
            var idJaapu  = userIds["jaapu"];
            var idSakke  = userIds["sakke"];
            var idTimo   = userIds["timo"];

            // Mökkilan 80 osallistujat (8 henkilöä)
            var mokkila80ParticipantIds = new List<string> { idJaapu, idLeivo, idTimo, idJHattu, idUrpi, idEpi, idSakke, idZeip };

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
                            OrganizerId = idJHattu, // JHattu
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Mökin ennakkomaksu", Quantity = 1, UnitPrice = 325.00m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Mökin loppulasku - 760,00 € - Maksaja: Jani (JHattu) - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Mökin loppulasku",
                            OrganizerId = idJHattu, // JHattu
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Mökin loppulasku", Quantity = 1, UnitPrice = 760.00m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Alko - 117,69 € - Maksaja: Zeip - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Alko",
                            OrganizerId = idZeip, // Zeip
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Alkoholijuomat", Quantity = 1, UnitPrice = 117.69m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Ruoat pl. burgerit - 190,79 € - Maksaja: Zeip - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Ruoat pl. burgerit",
                            OrganizerId = idZeip, // Zeip
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Ruokaostokset", Quantity = 1, UnitPrice = 190.79m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Lucifer sytytyspalat - 5,90 € - Maksaja: Epi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Lucifer sytytyspalat",
                            OrganizerId = idEpi, // Epi
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Sytytyspalat", Quantity = 1, UnitPrice = 5.90m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Taksi ravintolasta - 64,50 € - Maksaja: Leivo - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Taksi ravintolasta",
                            OrganizerId = idLeivo, // Leivo
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Taksimatka", Quantity = 1, UnitPrice = 64.50m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Jumperin bensat - 106,95 € - Maksaja: Leivo - Jaetaan vain Jarnolle (Jaapu) ja Leivolle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Gasoline,
                            Name = "Jumperin bensat (610 km)",
                            OrganizerId = idLeivo, // Leivo
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Bensiini 610 km, 10.6 l/100km, 1.654€/l", Quantity = 1, UnitPrice = 106.95m }
                            },
                            Payers =
                            [
                                new ExpenseItemPayer { AppUserId = idJaapu }, // Jaapu
                                new ExpenseItemPayer { AppUserId = idLeivo }  // Leivo
                            ]
                        },
                        // Taksi ravintolaan - 60,40 € - Maksaja: Urpi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Taksi ravintolaan",
                            OrganizerId = idUrpi, // Urpi
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Taksimatka", Quantity = 1, UnitPrice = 60.40m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Burgeri / ruoka 1 - 59,46 € - Maksaja: Urpi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.ShoppingList,
                            Name = "Burgeri / ruoka 1",
                            OrganizerId = idUrpi, // Urpi
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Burgerit", Quantity = 1, UnitPrice = 59.46m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        },
                        // Pyyhkeet, hiilet, jäät - 16,96 € - Maksaja: Urpi - Jaetaan kaikille 8:lle
                        new ExpenseItem
                        {
                            ExpenseType = ExpenseType.Personal,
                            Name = "Pyyhkeet, hiilet, jäät",
                            OrganizerId = idUrpi, // Urpi
                            LineItems = new List<ExpenseLineItem>
                            {
                                new ExpenseLineItem { Name = "Sekalaiset", Quantity = 1, UnitPrice = 16.96m }
                            },
                            Payers = [.. mokkila80ParticipantIds.Select(id => new ExpenseItemPayer { AppUserId = id })]
                        }
                    },
                    Participants = [.. mokkila80ParticipantIds.Select(id => new InvoiceParticipant { AppUserId = id })]
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