using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OptimizeMePlease.Context;
using OptimizeMePlease.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OptimizeMePlease
{
    [InProcess]
    [MemoryDiagnoser]
    public class BenchmarkService
    {
        PooledDbContextFactory<AppDbContext> _dbContextFactory;
        PooledDbContextFactory<AppDbContext> _indexedDbContextFactory;

        public BenchmarkService()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=OptimizeMePlease;Trusted_Connection=True;Integrated Security=true;MultipleActiveResultSets=true;Encrypt=false")
                .Options;

            _dbContextFactory = new PooledDbContextFactory<AppDbContext>(options);

            var indexedDbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=OptimizeMePlease-Indexed;Trusted_Connection=True;Integrated Security=true;MultipleActiveResultSets=true;Encrypt=false")
                .Options;

            _indexedDbContextFactory = new PooledDbContextFactory<AppDbContext>(indexedDbOptions);
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
            using var dbContext = _dbContextFactory.CreateDbContext();

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
            using var dbContext = _dbContextFactory.CreateDbContext();

            var authors =
                dbContext.Authors
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
            using var dbContext = _indexedDbContextFactory.CreateDbContext();

            var authors =
                dbContext.Authors
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

        const string dapperSql = """
            SELECT u.Created UserCreated,
                   u.EmailConfirmed UserEmailConfirmed,
                   u.FirstName UserFirstName,
                   u.LastActivity UserLastActivity,
                   u.LastName UserLastName,
                   u.Email UserEmail,
                   u.UserName UserName,
                   u.Id UserId,
                   (SELECT TOP 1 RoleId FROM UserRoles ur WHERE ur.UserId = a.UserId) RoleId,
                   a.BooksCount,
                   a.Age AuthorAge, a.Country AuthorCountry, a.NickName AuthorNickName, a.Id,
                   b.Id, b.Name, b.Published, b.ISBN, b.PublisherName, b.PublishedYear
              FROM Authors a
              JOIN Users u ON u.Id = a.UserId
              JOIN (
                    SELECT b.Id, b.Name, b.Published, b.ISBN, p.Name PublisherName, DATEPART(YEAR, b.Published) PublishedYear, b.AuthorId
                        FROM Books b
                        JOIN Publishers p ON p.Id = b.PublisherId
                    ) b ON b.AuthorId = a.Id AND PublishedYear < @publishedYear
             WHERE a.Country = @country AND a.Age = @age
             ORDER BY a.BooksCount, b.Id;
            """;
        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized_Dapper()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var queriedAuthors = new Dictionary<int, AuthorDTO>();

            var authors = dbContext.Database.GetDbConnection().Query<AuthorDTO, BookDto, AuthorDTO>(
                dapperSql,
                (author, book) =>
                {
                    if (!queriedAuthors.TryGetValue(author.Id, out var authorEntry))
                    {
                        authorEntry = author;
                        authorEntry.AllBooks ??= new List<BookDto>();
                        queriedAuthors.Add(authorEntry.Id, authorEntry);
                    }

                    if (!authorEntry.AllBooks.Any(b => b.Id == book.Id))
                    {
                        authorEntry.AllBooks.Add(book);
                    }

                    return authorEntry;
                },
                new { country = "Serbia", age = 27, publishedYear = 1900 }
                )
            .ToList();

            return queriedAuthors.Values.ToList();
        }

        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized_DapperIndexed()
        {
            using var dbContext = _indexedDbContextFactory.CreateDbContext();

            var queriedAuthors = new Dictionary<int, AuthorDTO>();

            var authors = dbContext.Database.GetDbConnection().Query<AuthorDTO, BookDto, AuthorDTO>(
                dapperSql,
                (author, book) =>
                {
                    if (!queriedAuthors.TryGetValue(author.Id, out var authorEntry))
                    {
                        authorEntry = author;
                        authorEntry.AllBooks ??= new List<BookDto>();
                        queriedAuthors.Add(authorEntry.Id, authorEntry);
                    }

                    if (!authorEntry.AllBooks.Any(b => b.Id == book.Id))
                    {
                        authorEntry.AllBooks.Add(book);
                    }

                    return authorEntry;
                },
                new { country = "Serbia", age = 27, publishedYear = 1900 }
                )
            .ToList();

            return queriedAuthors.Values.ToList();
        }

        /// <summary>
        /// Not my original work, copied and adapted from https://github.com/qjustfeelitp/OptimizeMePlease_Challange.
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public IList<AuthorDTO> GetAuthors_Optimized_Expression()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            return Get(dbContext).ToList();
        }
        [Benchmark]
        public IList<AuthorDTO> GetAuthors_Optimized_ExpressionIndexed()
        {
            using var dbContext = _indexedDbContextFactory.CreateDbContext();

            return Get(dbContext).ToList();
        }

        private const string Serbia = nameof(Serbia);
        private const int Age = 27;
        private const int Year = 1900;

        private static readonly Expression<Func<Author, bool>> AuthorWhereFilterExpression = author => (author.Country == Serbia) && (author.Age == Age);
        private static readonly Expression<Func<Book, bool>> BookWhereFilterExpression = book => book.Published < EF.Functions.DateFromParts(Year, 1, 1);

        private static readonly Expression<Func<Book, BookDto>> BookSelectorExpression = y => new BookDto
        {
            Id = y.Id,
            Name = y.Name,
            Published = y.Published,
            ISBN = y.ISBN,
            PublisherName = y.Publisher.Name,
            PublishedYear = y.Published.Year
        };

        private static readonly Expression<Func<Author, AuthorDTO>> AuthorDtoSelectorExpression = x => new AuthorDTO
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
            AllBooks = x.Books.AsQueryable()
                          .Where(BookWhereFilterExpression)
                          .Select(BookSelectorExpression)
                          .ToList(),
            AuthorAge = x.Age,
            AuthorCountry = x.Country,
            AuthorNickName = x.NickName,
            Id = x.Id
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
