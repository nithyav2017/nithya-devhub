# QueryBuilder Tool for .NET
**Overview
QueryBuilder is a lightweight utility for dynamically composing LINQ queries in Entity Framework Core. It simplifies filtering, sorting, and 
pagination logic.

### Example
var query = new QueryBuilder<User>(db.Users)
    .Where("Age", ">", 18)
    .OrderBy("LastName")
    .Skip(10)
    .Take(20)
    .Build()
    .ToList();
//Returns users over 18,  sorted by last name, paginated

### Motivation
Modern APIs often require dynamic filtering, sorting, and pagination. QueryBuilder standardizes this logic, reducing boilerplate and improving consistency across endpoints.

### Goals
- Provide a fluent API for dynamic query composition
- Support `Where`,`OrderBy`,`Skip`, and `Take` out of the box
- Allow JSON-based query definitions for frontend integration
- Keep the tool lightweight , dependency-free , and EF-Core friendly

### Non-Goals
- No support for complex joins or groupings , frontend integration , orderBy dynamic properties , advanced filters with nested properties in the initial release 
