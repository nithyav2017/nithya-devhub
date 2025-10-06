using Microsoft.EntityFrameworkCore;
using QueryBuilder;

namespace TestQueryBuilder
{
    public class Tests
    {
        private TestDbContext _context;
        
        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                            .UseInMemoryDatabase("TestDb")
                            .Options;
            _context = new TestDbContext(options);
            _context.Users.Add(new User { Id=1, Name = "Jack", Age = 40 , IsActive=true});
            _context.Users.Add(new User { Id=2, Name = "Glade", Age = 37, IsActive=true });
            _context.Users.Add(new User { Id=3, Name = "Sara", Age = 48, IsActive = false });
            _context.Users.Add(new User { Id = 4, Name = "Juli", Age = 52, IsActive = false });
            _context.SaveChanges();
        }

        [Test]
        public void QueryBuilder_FilterByAgeTest()
        {
            QueryBuilder<User> queryBuilder = new();

            var filter = queryBuilder
                            .AndWhere("Age", ">", 30)
                            .AndWhere("Age", "<", 50)
                            .AndWhere("IsActive", "==", true)
                            .Build();

            List<User>? users = _context.Users.Where(filter).ToList();

            Console.WriteLine("Users matched:");
            foreach (var user in users)
                Console.WriteLine($"{user.Name}, Age: {user.Age}, IsActive: {user.IsActive}");

            Assert.AreEqual(2, users.Count);
        }

        [TearDown]
        public void CleanUp()
        {
            _context?.Dispose();
        }
    }
}