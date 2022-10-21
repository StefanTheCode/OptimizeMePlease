using System;
using System.Collections.Generic;

namespace OptimizeMePlease
{
    public  class AuthorDTO
    {
        public DateTime UserCreated { get; set; }
        public bool UserEmailConfirmed { get; set; }
        public string UserFirstName { get; set; }
        public DateTime UserLastActivity { get; set; }
        public string UserLastName { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public int AuthorId { get; set; }
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int BooksCount { get; set; }
        public List<BookDto> AllBooks { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }
        public string AuthorNickName { get; set; }
    }

    public sealed class AuthorDTO_Optimized
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
        public IEnumerable<BookDTO_Optimized> Books { get; set; }
    }

    public struct AuthorDTO_OptimizedStruct
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
        public IEnumerable<BookDTO_OptimizedStruct> Books { get; set; }
    }

    public struct BookDTO_OptimizedStruct
    {
        public string Title { get; set; }
        public int PublishedYear { get; set; }
    }

    public sealed class BookDTO_Optimized
    {
        public string Title { get; set; }
        public DateTime Published { get; set; }
        public int PublishedYear { get; set; }
    }
}
