using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestQueryBuilder
{
    public class SalesOrder
    {
        public class Product
        {
            [Key]
            public int ProductID { get; set; }
            public string Name { get; set; } = default!;
            public decimal ListPrice { get; set; }
            public ICollection<SalesOrderDetail> SalesOrderDetails { get; set; }
       
        }

        public class SalesOrderDetail
        {
            [Key]
            public int SalesOrderDetailID { get; set; }
            public int SalesOrderID { get; set; }

            public decimal UnitPrice { get; set; }
            public int  OrderQty { get; set; }
            public int ProductID { get; set; }
            public  Product  Product { get; set; } = default!;
            public SalesOrderHeader salesOrderHeader { get; set; }
        }

        public class SalesOrderHeader
        {
            [Key]
            public int SalesOrderID { get; set; }
            public DateTime OrderDate { get; set; }
            public ICollection<SalesOrderDetail> SalesOrderDetails { get; set; }
        }
    }
}
