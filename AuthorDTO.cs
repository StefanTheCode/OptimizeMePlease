using System;
using System.Collections.Generic;
using static OptimizeMePlease.BenchmarkService;

namespace OptimizeMePlease
{
    public class AuthorDTO
    {
        public DateTime UserCreated { get; set; }
        public bool UserEmailConfirmed { get; set; }
        public string UserFirstName { get; set; }
        public DateTime UserLastActivity { get; set; }
        public string UserLastName { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int BooksCount { get; set; }
        public List<BookDto> AllBooks { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }
        public string AuthorNickName { get; set; }
    }

    public sealed class AuthorDtoOptimized
    {
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int BooksCount { get; set; }
        public ICollection<BookDtoOptimized> Books { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
    }

    public sealed record AuthorRecordDto(int Age, string Country, int BooksCount, string UserName, string FirstName, string LastName, string Email, ICollection<BookRecordDto> Books);
}
