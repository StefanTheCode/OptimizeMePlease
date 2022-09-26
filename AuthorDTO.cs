﻿using System;
using System.Collections.Generic;

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
        public int AuthorId { get; set; }
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int BooksCount { get; set; }
        public List<BookDto> AllBooks { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }
        public string AuthorNickName { get; set; }
    }

    public class AuthorDTO_Optimized
    {
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public int BooksCount { get; set; }
        public IEnumerable<BookDto> AllBooks { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }
    }
}
