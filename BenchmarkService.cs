using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Query;
using OptimizeMePlease.Context;
using OptimizeMePlease.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Columns;

namespace OptimizeMePlease
{
    public class TopAuthorInfo
    {

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public string Country { get; set; }
        public string Name { get; set; }
        public DateTime Published { get; set; }

    }

    public class NotModifyingOriginalDbContext : AppDbContext
    {

        public DbSet<TopAuthorInfo> TopAuthors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {  
            modelBuilder.Entity<TopAuthorInfo>()
           .HasNoKey()
           .ToTable((string)null);
            base.OnModelCreating(modelBuilder);
        }

    }
    [Config(typeof(Config))]
    [MemoryDiagnoser]
    public class BenchmarkService
    {
        // private const string sql = "SELECT top(2) a.Id, u.Id as UserId,u.FirstName, u.LastName, u.UserName, u.Email, a.Age, a.Country\r\nFROM Authors a\r\ninner join [Users] u on a.UserId = u.Id\r\nWHERE a.Age = 27 and a.Country = 'Serbia' \r\norder by a.BooksCount desc";

        private const string newSql = "select a.*,b.[Name], b.Published from (SELECT top(2) a.Id, u.Id as UserId,u.FirstName, u.LastName, u.UserName, u.Email, a.Age, a.Country FROM Authors a inner join [Users] u on a.UserId = u.Id WHERE a.Age = 27 and a.Country = 'Serbia'order by a.BooksCount desc) a inner JOIN [Books] b ON a.[Id] = b.AuthorId WHERE b.Published < '1900-01-01' order by a.Id";
        public BenchmarkService()
        {
        }


        /// <summary>
        /// Get top 2 Authors (FirstName, LastName, UserName, Email, Age, Country) 
        /// from country Serbia aged 27, with the highest BooksCount
        /// and all his/her books (Book Name/Title and Publishment Year) published before 1900
        /// </summary>
        /// <returns></returns>
        [Benchmark(Baseline = true)]
        public List<AuthorDTO> GetAuthors()
        {
            using var dbContext = new AppDbContext();

            var authors = dbContext.Authors
                                        .Include(x => x.User)
                                        .ThenInclude(x => x.UserRoles)
                                        .ThenInclude(x => x.Role)
                                        .Include(x => x.Books)
                                        .ThenInclude(x => x.Publisher)
                                        .ToList()
                                        .Select(x => new AuthorDTO
                                        {
                                            UserCreated = x.User.Created,
                                            UserEmailConfirmed = x.User.EmailConfirmed,
                                            UserFirstName = x.User.FirstName,
                                            UserLastActivity = x.User.LastActivity,
                                            UserLastName = x.User.LastName,
                                            UserEmail = x.User.Email,
                                            UserName = x.User.UserName,
                                            UserId = x.User.Id,
                                            RoleId = x.User.UserRoles.FirstOrDefault(y => y.UserId == x.UserId).RoleId,
                                            BooksCount = x.BooksCount,
                                            AllBooks = x.Books.Select(y => new BookDto
                                            {
                                                Id = y.Id,
                                                Name = y.Name,
                                                Published = y.Published,
                                                ISBN = y.ISBN,
                                                PublisherName = y.Publisher.Name
                                            }).ToList(),
                                            AuthorAge = x.Age,
                                            AuthorCountry = x.Country,
                                            AuthorNickName = x.NickName,
                                            Id = x.Id
                                        })
                                        .ToList()
                                        .Where(x => x.AuthorCountry == "Serbia" && x.AuthorAge == 27)
                                        .ToList();

            var orderedAuthors = authors.OrderByDescending(x => x.BooksCount).ToList().Take(2).ToList();

            List<AuthorDTO> finalAuthors = new List<AuthorDTO>();
            foreach (var author in orderedAuthors)
            {
                List<BookDto> books = new List<BookDto>();

                var allBooks = author.AllBooks;

                foreach (var book in allBooks)
                {
                    if (book.Published.Year < 1900)
                    {
                        book.PublishedYear = book.Published.Year;
                        books.Add(book);
                    }
                }

                author.AllBooks = books;
                finalAuthors.Add(author);
            }

            return finalAuthors;
        }

        [Benchmark]
        public List<AuthorDtoOptimised> GetAuthors_Optimized()
        {

            using var dbContext = new NotModifyingOriginalDbContext();
            var topAuthorsQuery = dbContext.TopAuthors.FromSqlRaw(newSql).AsNoTracking();

            var results = topAuthorsQuery.AsEnumerable();
            var authors = new List<AuthorDtoOptimised>();

            AuthorDtoOptimised currentAuthor = null;
            foreach (var item in results)
            {
                if (item.Id != currentAuthor?.Id)
                {
                    currentAuthor = new AuthorDtoOptimised()
                    {
                        Id = item.Id,
                        UserFirstName = item.FirstName,
                        UserLastName = item.LastName,
                        UserName = item.UserName,
                        UserEmail = item.Email,
                        AuthorAge = item.Age,
                        AuthorCountry = item.Country                                             
                    };
                    currentAuthor.AllBooks.Add(new BookDtoOptimised() { Name = item.Name, Published = item.Published });
                    authors.Add(currentAuthor);
                    continue;
                }

                currentAuthor.AllBooks.Add(new BookDtoOptimised() { Name = item.Name, Published = item.Published });
            }

            // var queryString = query.ToQueryString();            
            return authors;
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
            }
        }
    }

    public class BookDtoOptimised
    {
        public string Name { get; set; }
        public DateTime Published { get; set; }
    }

    public class AuthorDtoOptimised
    {
        public int Id { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public int AuthorAge { get; set; }
        public string AuthorCountry { get; set; }

        public List<BookDtoOptimised> AllBooks { get; set; } = new List<BookDtoOptimised>();
    }
}
