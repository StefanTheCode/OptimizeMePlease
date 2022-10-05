using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;
using OptimizeMePlease.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OptimizeMePlease
{
    [InProcess]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class BenchmarkService
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
            var authors =
                ContextProvider.AppDbContext.Authors
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
                        RoleId = x.User.UserRoles.FirstOrDefault().RoleId,
                        BooksCount = x.BooksCount,
                        AllBooks = x.Books.Select(y => new BookDto
                        {
                            Id = y.Id,
                            Name = y.Name,
                            Published = y.Published,
                            ISBN = y.ISBN,
                            PublisherName = y.Publisher.Name,
                            PublishedYear = y.Published.Year
                        }).Where(b => b.Published.Year < 1900).ToList(),
                        AuthorAge = x.Age,
                        AuthorCountry = x.Country,
                        AuthorNickName = x.NickName,
                        Id = x.Id
                    })
                    .Where(x => x.AuthorCountry == "Serbia" && x.AuthorAge == 27);

            var orderedAuthors = authors.OrderByDescending(x => x.BooksCount).Take(2);

            return orderedAuthors.ToList();
        }

        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized_Indexed()
        {
            var authors =
                IndexedContextProvider.AppDbContext.Authors
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
                        RoleId = x.User.UserRoles.FirstOrDefault().RoleId,
                        BooksCount = x.BooksCount,
                        AllBooks = x.Books.Select(y => new BookDto
                        {
                            Id = y.Id,
                            Name = y.Name,
                            Published = y.Published,
                            ISBN = y.ISBN,
                            PublisherName = y.Publisher.Name,
                            PublishedYear = y.Published.Year
                        }).Where(b => b.Published.Year < 1900).ToList(),
                        AuthorAge = x.Age,
                        AuthorCountry = x.Country,
                        AuthorNickName = x.NickName,
                        Id = x.Id
                    })
                    .Where(x => x.AuthorCountry == "Serbia" && x.AuthorAge == 27);

            var orderedAuthors = authors.OrderByDescending(x => x.BooksCount).Take(2);

            return orderedAuthors.ToList();
        }

        [Benchmark]
        public IList<AuthorDTO> GetAuthors_Optimized_Expression()
        {
            var db = ContextProvider.AppDbContext;

            return Get(db).ToList();
        }

        private const string Serbia = nameof(Serbia);
        private const int Age = 27;
        private const int Year = 1900;

        private static readonly Expression<Func<Author, bool>> AuthorWhereFilterExpression = author => (author.Country == Serbia) && (author.Age == Age);
        private static readonly Expression<Func<Book, bool>> BookWhereFilterExpression = book => book.Published < EF.Functions.DateFromParts(Year, 1, 1);

        private static readonly Expression<Func<Book, BookDto>> BookSelectorExpression = book => new BookDto
        {
            Name = book.Name,
            PublishedYear = book.Published.Year
        };

        private static readonly Expression<Func<Author, AuthorDTO>> AuthorDtoSelectorExpression = author => new AuthorDTO
        {
            UserFirstName = author.User.FirstName,
            UserLastName = author.User.LastName,
            UserEmail = author.User.Email,
            UserName = author.User.UserName,
            BooksCount = author.BooksCount,
            AllBooks = author.Books.AsQueryable()
                          .Where(BookWhereFilterExpression)
                          .Select(BookSelectorExpression)
                          .ToList(),
            AuthorAge = author.Age,
            AuthorCountry = author.Country
        };

        private static readonly Func<DbContext, IEnumerable<AuthorDTO>> Get =
            EF.CompileQuery((DbContext db) =>
                                db.Set<Author>()
                                  .Where(AuthorWhereFilterExpression)
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(AuthorDtoSelectorExpression));
    }
}
