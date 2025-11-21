using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static TestQueryBuilder.SalesOrder;

namespace TestQueryBuilder
{
    public class TestDbContext : DbContext
    {
 
        public DbSet<Product> Products { get; set; }
        public DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
        public DbSet<SalesOrderHeader> SalesOrderHeaders { get; set; }


        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define MyViewModel as keyless
           

            modelBuilder.Entity<Product>().ToTable("Product", "Production").HasKey(p => p.ProductID);
            modelBuilder.Entity<SalesOrderDetail>().ToTable("SalesOrderDetail", "Sales").HasKey(sod => sod.SalesOrderDetailID);
            modelBuilder.Entity<SalesOrderHeader>().ToTable("SalesOrderHeader", "Sales").HasKey(soh => soh.SalesOrderID);

            // Define relationships

            modelBuilder.Entity<SalesOrderDetail>()
                .HasOne(sod => sod.Product)
                .WithMany(p => p.SalesOrderDetails)
                .HasForeignKey(sod => sod.ProductID);

            modelBuilder.Entity<SalesOrderDetail>()
                .HasOne(d => d.salesOrderHeader)
                .WithMany(h => h.SalesOrderDetails)
                .HasForeignKey(d => d.SalesOrderID);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.SalesOrderDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductID);


            base.OnModelCreating(modelBuilder);
        }
    }
}
