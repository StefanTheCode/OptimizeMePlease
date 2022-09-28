using System.Collections.Generic;

namespace OptimizeMePlease
{
    public class AuthorDTO_Optimized
    {
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }
        public IEnumerable<BookDto_Optimized> AllBooks { get; set; }
    }
}
