 using System.ComponentModel.DataAnnotations;
 

namespace TestQueryBuilder
{
    public class User
    {
        [Key]
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int? Age { get; set; }
        public bool? IsActive { get; set; }
    }
}
