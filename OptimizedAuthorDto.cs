using System.Collections.Generic;

namespace OptimizeMePlease
{
    public class OptimizedAuthorDTO
    {
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }
        public List<BookDto> AllBooks { get; set; }
    }
}
