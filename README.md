# QueryBuilder Tool for .NET
### Overview
QueryBuilder is a lightweight utility for dynamically composing LINQ queries in Entity Framework Core. It simplifies filtering, sorting, and 
pagination logic.

### Example [Initial Release]
var query = new QueryBuilder<User>(db.Users)
    .Where("Age", ">", 18)
    .OrderBy("LastName")
    .Skip(10)
    .Take(20)
    .Build()
    .ToList();
//Returns users over 18,  sorted by last name, paginated

### Example [Latest]
 var filter = queryBuilder                           
                .AddJoin(typeof(SalesOrder.SalesOrderDetail), "ProductID", "ProductID", "Inner")
                .AddJoin(typeof(SalesOrder.SalesOrderHeader), "SalesOrderID", "SalesOrderID", "Inner")
                .AndWhere("Name", "StartsWith", "Road")
                .AndWhere("UnitPrice", ">", 2000M, typeof(SalesOrder.SalesOrderDetail))
                .OrElse("OrderQty", ">", 20 , typeof(SalesOrder.SalesOrderDetail)) 
                .AndWhere("SalesOrderDetailID", "IN", (115,156), typeof(SalesOrder.SalesOrderDetail))
                .Build(_context.Products, _context).Cast<dynamic>() ;
### Query Output
     SELECT [p].[ProductID], [p].[ListPrice], [p].[Name], [s0].[SalesOrderDetailID], [s0].[OrderQty], [s0].[ProductID], 
 [s0].[SalesOrderID], [s0].[UnitPrice], [s1].[SalesOrderID], [s1].[OrderDate]
      FROM [Production].[Product] AS [p]
      INNER JOIN (
          SELECT [s].[SalesOrderDetailID], [s].[OrderQty], [s].[ProductID], [s].[SalesOrderID], [s].[UnitPrice]
          FROM [Sales].[SalesOrderDetail] AS [s]
          WHERE ([s].[UnitPrice] > 2000.0 OR [s].[OrderQty] > 20) AND [s].[SalesOrderDetailID] IN (115, 156)
      ) AS [s0] ON [p].[ProductID] = [s0].[ProductID]
      INNER JOIN [Sales].[SalesOrderHeader] AS [s1] ON [s0].[SalesOrderID] = [s1].[SalesOrderID]
      WHERE [p].[Name] LIKE N'Road%'


### Motivation
Modern APIs often require dynamic filtering, sorting, and pagination. QueryBuilder standardizes this logic, reducing boilerplate and improving consistency across endpoints.

### Goals
- Provide a fluent API for dynamic query composition
- Support `Where`,`OR`,`Skip`, and `Take` out of the box
- Support joins Inner,Left,Right,Full Outer
- Support Groupby, Aggregation, Order By 
- Allow JSON-based query definitions for frontend integration(API Integration)
- Validation Layer
- Export query into a portable format(JSON/XML) for logging and Debugging
- SQL Injection safety, Query caching and Async support
- Keep the tool lightweight , dependency-free , and EF-Core friendly
### New Features 
-Reusable Dynamic Joins
    -Supports multiple entity joins(Inner) with type safe LINQ expressions.
    -Process the query in the Database instead of In-Memory
    -Supports filter with "==",">=","<=",">","<","!=", "IN","NOT IN", "LIKE" 
-Expression Tree Validation
    -Clear error handling for type mismatches in Property/parameter expressions.
    -
### Non-Goals [Initial Release Limitations]
- No support for complex joins or groupings , frontend integration , orderBy dynamic properties , advanced filters with nested properties in the initial release

## Contribution Guidelines
This is a small , evolving project. If you'd like to contribute:
- Feel free to fork the repo and submit a pull request with clear description
- No formal release or branching rules yet - Please keep the 'master' branch stable
- If you are adding new features, consider updating the README with example
- For questions or ideas , feel free to open an issue.
## Coding Standards
- Use .NET 8.0/C# 12

  


