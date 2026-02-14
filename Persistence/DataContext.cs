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
        public DbSet<ExpenseLineItem> ExpenseLineItems { get; set; }
        // public DbSet<Creditor> Creditors { get; set; } // Removed
        public DbSet<InvoiceParticipant> InvoiceParticipants { get; set; }
        public DbSet<ExpenseItemPayer> ExpenseItemPayers { get; set; }
        public DbSet<InvoiceApproval> InvoiceApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Invoice -> InvoiceParticipant relationship with cascade delete
            modelBuilder.Entity<InvoiceParticipant>()
                .HasKey(ip => new { ip.InvoiceId, ip.AppUserId });

            modelBuilder.Entity<InvoiceParticipant>()
                .HasOne(ip => ip.Invoice)
                .WithMany(i => i.Participants)
                .HasForeignKey(ip => ip.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceParticipant>()
                .HasOne(ip => ip.AppUser)
                .WithMany(u => u.Invoices)
                .HasForeignKey(ip => ip.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExpenseItem -> ExpenseItemPayer relationship with cascade delete
            modelBuilder.Entity<ExpenseItemPayer>()
                .HasKey(eip => new { eip.ExpenseItemId, eip.AppUserId });

            modelBuilder.Entity<ExpenseItemPayer>()
                .HasOne(eip => eip.ExpenseItem)
                .WithMany(ei => ei.Payers)
                .HasForeignKey(eip => eip.ExpenseItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExpenseItemPayer>()
                .HasOne(eip => eip.AppUser)
                .WithMany(u => u.ExpenseItemPayers)
                .HasForeignKey(eip => eip.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Invoice -> ExpenseItem relationship with cascade delete
            modelBuilder.Entity<ExpenseItem>()
                .HasOne<Invoice>()
                .WithMany(i => i.ExpenseItems)
                .HasForeignKey(ei => ei.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExpenseItem -> Organizer relationship with cascade delete
            modelBuilder.Entity<ExpenseItem>()
                .HasOne(ei => ei.Organizer)
                .WithMany()
                .HasForeignKey(ei => ei.OrganizerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExpenseItem -> ExpenseLineItem relationship with cascade delete
            modelBuilder.Entity<ExpenseLineItem>()
                .HasOne(eli => eli.ExpenseItem)
                .WithMany(ei => ei.LineItems)
                .HasForeignKey(eli => eli.ExpenseItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Invoice -> InvoiceApproval relationship with cascade delete
            modelBuilder.Entity<InvoiceApproval>()
                .HasKey(ia => new { ia.InvoiceId, ia.AppUserId });

            modelBuilder.Entity<InvoiceApproval>()
                .HasOne(ia => ia.Invoice)
                .WithMany(i => i.Approvals)
                .HasForeignKey(ia => ia.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceApproval>()
                .HasOne(ia => ia.AppUser)
                .WithMany()
                .HasForeignKey(ia => ia.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}