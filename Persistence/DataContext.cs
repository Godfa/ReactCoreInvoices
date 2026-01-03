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
        public DbSet<ExpenseItemPayer> ExpenseItemPayers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Invoice -> InvoiceParticipant relationship with cascade delete
            modelBuilder.Entity<InvoiceParticipant>()
                .HasKey(ip => new { ip.InvoiceId, ip.CreditorId });

            modelBuilder.Entity<InvoiceParticipant>()
                .HasOne(ip => ip.Invoice)
                .WithMany(i => i.Participants)
                .HasForeignKey(ip => ip.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceParticipant>()
                .HasOne(ip => ip.Creditor)
                .WithMany(c => c.Invoices)
                .HasForeignKey(ip => ip.CreditorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExpenseItem -> ExpenseItemPayer relationship with cascade delete
            modelBuilder.Entity<ExpenseItemPayer>()
                .HasKey(eip => new { eip.ExpenseItemId, eip.CreditorId });

            modelBuilder.Entity<ExpenseItemPayer>()
                .HasOne(eip => eip.ExpenseItem)
                .WithMany(ei => ei.Payers)
                .HasForeignKey(eip => eip.ExpenseItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExpenseItemPayer>()
                .HasOne(eip => eip.Creditor)
                .WithMany(c => c.ExpenseItems)
                .HasForeignKey(eip => eip.CreditorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Invoice -> ExpenseItem relationship with cascade delete
            modelBuilder.Entity<ExpenseItem>()
                .HasOne<Invoice>()
                .WithMany(i => i.ExpenseItems)
                .HasForeignKey("InvoiceId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}