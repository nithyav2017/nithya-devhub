using Microsoft.EntityFrameworkCore;
using QueryBuilder;
using System;
using System.Dynamic;
using System.Net;
 
namespace TestQueryBuilder
{
    public class Tests
    {
        private TestDbContext _context;
        
        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                            .UseSqlServer("Server=localhost, 1433;Database=AdventureWorks2022;User Id=sa;Password=Learning@12;TrustServerCertificate=True")
                            .Options;
            _context = new TestDbContext(options); 

        }

        [Test]
        public void QueryBuilder_FilterByAgeTest()
        {
            QueryBuilder<SalesOrder.Product> queryBuilder = new();

            _context.Products.AsEnumerable();
            _context.SalesOrderDetails.AsEnumerable();
            _context.SalesOrderHeaders.AsEnumerable();

                var filter = queryBuilder                           
                               .AddJoin(typeof(SalesOrder.SalesOrderDetail), "ProductID", "ProductID", "Inner")
                               .AddJoin(typeof(SalesOrder.SalesOrderHeader), "SalesOrderID", "SalesOrderID", "Inner")
                               .AndWhere("Name", "Contains", "Black")
                               .AndWhere("UnitPrice", ">", 2000M, typeof(SalesOrder.SalesOrderDetail))
                               .OrElse("OrderQty", ">", 20 , typeof(SalesOrder.SalesOrderDetail)) 
                               .AndWhere("SalesOrderDetailID", "IN", (115,156), typeof(SalesOrder.SalesOrderDetail))
                               .Build(_context.Products, _context).Cast<dynamic>() ;
                           
 
            var sql = filter.ToQueryString(); 
            
            Assert.AreEqual(2, filter.Count());
        }

        [TearDown]
        public void CleanUp()
        {
            _context?.Dispose();
        }
    }
}