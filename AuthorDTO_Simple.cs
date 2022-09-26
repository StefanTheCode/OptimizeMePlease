using System.Collections.Generic;

namespace OptimizeMePlease
{
    public class AuthorDTO_Simple
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
        public string NickName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public int BooksCount { get; set; }
        public IEnumerable<BookDto_Meaningful> AllBooks { get; set; }

    }
}
