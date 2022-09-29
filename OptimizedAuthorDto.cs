using System.Collections.Generic;

namespace OptimizeMePlease
{
    public class OptimizedAuthorDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
        public IEnumerable<OptimizedBookDto> Books { get; set; }
    }
}
