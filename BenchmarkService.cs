using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;
using System.Collections.Generic;
using System.Linq;

namespace OptimizeMePlease
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public partial class BenchmarkService
    {
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
        public List<AuthorDTO> GetAuthors_Optimized()
        {
            using var dbContext = new AppDbContext();

            var authors = (from author in dbContext.Authors
                    where author.Country == "Serbia" && author.Age == 27
                    orderby author.BooksCount descending
                    select new AuthorDTO
                    {
                        UserFirstName = author.User.FirstName,
                        UserLastName = author.User.LastName,
                        UserName = author.User.UserName,
                        UserEmail = author.User.Email,
                        AuthorAge = author.Age,
                        AuthorCountry = author.Country,
                        AllBooks = author.Books.Select(book => new BookDto
                        {
                            Id = book.Id,
                            Name = book.Name,
                            Published = book.Published
                        }).ToList()
                    })
                    .AsNoTracking()
                    .Take(2)
                    .ToList();

            authors.ForEach(author =>
            {
                author.AllBooks = author.AllBooks.Where(book => book.Published.Year < 1900).ToList();
            });

            return authors;
        }
    
    }
}
