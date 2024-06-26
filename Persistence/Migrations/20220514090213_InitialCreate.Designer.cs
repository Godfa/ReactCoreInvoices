﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Persistence;

#nullable disable

namespace Persistence.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20220514090213_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.5");

            modelBuilder.Entity("Domain.ExpenseItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExpenseCreditor")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExpenseType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InvoiceGuid")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("InvoiceId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("InvoiceId");

                    b.ToTable("ExpenseItem");
                });

            modelBuilder.Entity("Domain.Invoice", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<int>("LanNumber")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Invoices");
                });

            modelBuilder.Entity("Domain.ExpenseItem", b =>
                {
                    b.HasOne("Domain.Invoice", null)
                        .WithMany("ExpenseItems")
                        .HasForeignKey("InvoiceId");
                });

            modelBuilder.Entity("Domain.Invoice", b =>
                {
                    b.Navigation("ExpenseItems");
                });
#pragma warning restore 612, 618
        }
    }
}
