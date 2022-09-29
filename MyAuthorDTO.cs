using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizeMePlease
{
    public class MyAuthorDTO
    {
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }
        public IEnumerable<MyBookDTO> AllBooks { get; set; }
    }
}
