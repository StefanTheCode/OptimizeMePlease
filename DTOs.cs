using System.Collections.Generic;

namespace OptimizeMePlease
{
    public class AuthorDto_Optimized
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int UserId { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
        public ICollection<BookStruct> Books { get; set; }
    }

    public struct BookStruct
    {
        public int AuthorId { get; set; }
        public string Title { get; set; }
        public int PublishedYear { get; set; }
    }
}