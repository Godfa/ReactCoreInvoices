using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence
{
    public class DataContext : IdentityDbContext<User>
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<ExpenseItem> ExpenseItems { get; set; }
        public DbSet<Creditor> Creditors { get; set; }
        public DbSet<InvoiceParticipant> InvoiceParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InvoiceParticipant>()
                .HasKey(ip => new { ip.InvoiceId, ip.CreditorId });

            modelBuilder.Entity<InvoiceParticipant>()
                .HasOne(ip => ip.Invoice)
                .WithMany(i => i.Participants)
                .HasForeignKey(ip => ip.InvoiceId);

            modelBuilder.Entity<InvoiceParticipant>()
                .HasOne(ip => ip.Creditor)
                .WithMany(c => c.Invoices)
                .HasForeignKey(ip => ip.CreditorId);
        }
    }
}