using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CompiledModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OptimizeMePlease.Context;
using OptimizeMePlease.Entities;

namespace OptimizeMePlease
{
    [MemoryDiagnoser(false)]
    //[InProcess]
    public class BenchmarkService
    {
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
                                                     })
                                                    .ToList(),
                                        AuthorAge = x.Age,
                                        AuthorCountry = x.Country,
                                        AuthorNickName = x.NickName,
                                        Id = x.Id
                                    })
                                   .ToList()
                                   .Where(x => (x.AuthorCountry == "Serbia") && (x.AuthorAge == 27))
                                   .ToList();

            var orderedAuthors = authors.OrderByDescending(x => x.BooksCount).ToList().Take(2).ToList();

            var finalAuthors = new List<AuthorDTO>();

            foreach (var author in orderedAuthors)
            {
                var books = new List<BookDto>();

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
        public IList<AuthorDtoOptimized> GetAuthors_Optimized()
        {
            using var db = ContextFactory.CreateDbContext();

            return Get(db).ToList();
        }

        [Benchmark]
        public IList<AuthorDtoOptimized> GetAuthors_OptimizedInline()
        {
            using var db = ContextFactory.CreateDbContext();

            return db.Set<Author>()
                     .Where(author => (author.Country == Serbia) && (author.Age == Age))
                     .OrderByDescending(x => x.BooksCount)
                     .Take(2)
                     .Select(author => new AuthorDtoOptimized
                      {
                          UserFirstName = author.User.FirstName,
                          UserLastName = author.User.LastName,
                          UserEmail = author.User.Email,
                          UserName = author.User.UserName,
                          BooksCount = author.BooksCount,
                          Books = author.Books.AsQueryable()
                                        .Where(book => book.Published < EF.Functions.DateFromParts(Year, 1, 1))
                                        .Select(y => new BookDtoOptimized
                                         {
                                             Name = y.Name,
                                             PublishedYear = y.Published.Year
                                         })
                                        .ToList(),
                          Age = author.Age,
                          Country = author.Country
                      })
                     .ToList();
        }

        [Benchmark]
        public IList<AuthorDtoOptimized> GetAuthors_OptimizedInlineWithParameters()
        {
            using var db = ContextFactory.CreateDbContext();

            string country = Serbia;
            int age = Age;
            int year = Year;

            return db.Set<Author>()
                     .Where(author => (author.Country == country) && (author.Age == age))
                     .OrderByDescending(x => x.BooksCount)
                     .Take(2)
                     .Select(author => new AuthorDtoOptimized
                      {
                          UserFirstName = author.User.FirstName,
                          UserLastName = author.User.LastName,
                          UserEmail = author.User.Email,
                          UserName = author.User.UserName,
                          BooksCount = author.BooksCount,
                          Books = author.Books.AsQueryable()
                                        .Where(book => book.Published < EF.Functions.DateFromParts(year, 1, 1))
                                        .Select(y => new BookDtoOptimized
                                         {
                                             Name = y.Name,
                                             PublishedYear = y.Published.Year
                                         })
                                        .ToList(),
                          Age = author.Age,
                          Country = author.Country
                      })
                     .ToList();
        }

        [Benchmark]
        public IList<AuthorDtoOptimized> GetAuthors_OptimizedWithoutExpressions()
        {
            using var db = ContextFactory.CreateDbContext();

            return GetWithoutExpressions(db).ToList();
        }

        [Benchmark]
        public async Task<AuthorDtoOptimized[]> GetAuthors_OptimizedAsync()
        {
            await using var db = await ContextFactory.CreateDbContextAsync();

            var authors = new AuthorDtoOptimized[2];
            int counter = 0;

            await foreach (var author in GetAsync(db))
            {
                authors[counter] = author;
                counter++;
            }

            return authors;
        }

        [Benchmark]
        public async IAsyncEnumerable<AuthorDtoOptimized> GetAuthors_OptimizedAsyncPassThrough()
        {
            await using var db = await ContextFactory.CreateDbContextAsync();

            await foreach (var author in GetAsync(db))
            {
                yield return author;
            }
        }

        [Benchmark]
        public IList<AuthorDtoOptimized> GetAuthors_OptimizedParameterized()
        {
            using var db = ContextFactory.CreateDbContext();

            return GetParameterized(db, Serbia, Age).ToList();
        }

        [Benchmark]
        public Span<AuthorRecordDto> GetAuthors_OptimizedRecordSpan()
        {
            using var db = ContextFactory.CreateDbContext();

            var array = ArrayPool.Rent(2);
            int counter = 0;

            foreach (var dto in GetRecord(db))
            {
                array[counter] = dto;
                counter++;
            }

            var result = array.AsSpan();
            ArrayPool.Return(array);

            return result;
        }

        [Benchmark]
        public IList<AuthorRecordDto> GetAuthors_OptimizedRecordList()
        {
            using var db = ContextFactory.CreateDbContext();

            return GetRecord(db).ToList();
        }

        [Benchmark]
        public IList<AuthorRecordDto> GetAuthors_OptimizedRecordListInline()
        {
            using var db = ContextFactory.CreateDbContext();

            return db.Set<Author>()
                     .Where(author => (author.Country == Serbia) && (author.Age == Age))
                     .OrderByDescending(x => x.BooksCount)
                     .Take(2)
                     .Select(author =>
                                 new AuthorRecordDto(author.Age,
                                                     author.Country,
                                                     author.BooksCount,
                                                     author.User.UserName,
                                                     author.User.FirstName,
                                                     author.User.LastName,
                                                     author.User.Email,
                                                     author.Books.AsQueryable()
                                                           .Where(book => book.Published < EF.Functions.DateFromParts(Year, 1, 1))
                                                           .Select(y => new BookRecordDto(y.Name, y.Published.Year))
                                                           .ToList()))
                     .ToList();
        }

        [Benchmark]
        public IList<AuthorRecordDto> GetAuthors_OptimizedRecordListInlineWithParameters()
        {
            using var db = ContextFactory.CreateDbContext();

            string country = Serbia;
            int age = Age;
            int year = Year;

            return db.Set<Author>()
                     .Where(author => (author.Country == country) && (author.Age == age))
                     .OrderByDescending(x => x.BooksCount)
                     .Take(2)
                     .Select(author =>
                                 new AuthorRecordDto(author.Age,
                                                     author.Country,
                                                     author.BooksCount,
                                                     author.User.UserName,
                                                     author.User.FirstName,
                                                     author.User.LastName,
                                                     author.User.Email,
                                                     author.Books.AsQueryable()
                                                           .Where(book => book.Published < EF.Functions.DateFromParts(year, 1, 1))
                                                           .Select(y => new BookRecordDto(y.Name, y.Published.Year))
                                                           .ToList()))
                     .ToList();
        }

        [Benchmark]
        public IList<AuthorRecordDto> GetAuthors_OptimizedRecordListWithoutExpressions()
        {
            using var db = ContextFactory.CreateDbContext();

            return GetRecordWithoutExpressions(db).ToList();
        }

        [Benchmark]
        public async Task<AuthorRecordDto[]> GetAuthors_OptimizedRecordAsync()
        {
            await using var db = await ContextFactory.CreateDbContextAsync();

            var authors = new AuthorRecordDto[2];
            int counter = 0;

            await foreach (var author in GetRecordAsync(db))
            {
                authors[counter] = author;
                counter++;
            }

            return authors;
        }

        [Benchmark]
        public async IAsyncEnumerable<AuthorRecordDto> GetAuthors_OptimizedRecordAsyncPassThrough()
        {
            await using var db = await ContextFactory.CreateDbContextAsync();

            await foreach (var author in GetRecordAsync(db))
            {
                yield return author;
            }
        }

        [Benchmark]
        public IList<AuthorRecordDto> GetAuthors_OptimizedRecordListParameterized()
        {
            using var db = ContextFactory.CreateDbContext();

            return GetRecordParameterized(db, Serbia, Age).ToList();
        }

        [Benchmark]
        public AuthorRecordDto[] GetAuthors_RawSqlArray()
        {
            using var db = ContextFactory.CreateDbContext();
            using var command = db.Database.GetDbConnection().CreateCommand();

            command.CommandText = CommandText;

            var countryParameter = new SqlParameter("@country", Serbia)
            {
                SqlDbType = SqlDbType.NVarChar
            };

            command.Parameters.Add(countryParameter);
            command.Parameters.Add(new SqlParameter("@take", 2));
            command.Parameters.Add(new SqlParameter("@age", Age));
            command.Parameters.Add(new SqlParameter("@year", 1900));

            return ProcessRawSqlResult(db, command);
        }

        [Benchmark]
        public AuthorRecordDto[] GetAuthors_RawSqlWithoutParametersArray()
        {
            using var db = ContextFactory.CreateDbContext();
            using var command = db.Database.GetDbConnection().CreateCommand();

            command.CommandText = CommandTextWithoutParameters;

            return ProcessRawSqlResult(db, command);
        }

        private static AuthorRecordDto[] ProcessRawSqlResult(DbContext db, DbCommand command)
        {
            db.Database.OpenConnection();
            using var reader = command.ExecuteReader();
            var authors = new List<AuthorRecordDto>();

            while (reader.Read())
            {
                //SELECT [u].[FirstName], [u].[LastName], [u].[Email], [u].[UserName], [t].[BooksCount], [t].[Id], [u].[Id], [t0].[Name], [t0].[PublishedYear], [t0].[Id], [t].[Age], [t].[Country]
                //(int Age, string Country, int BooksCount, string UserName, string FirstName, string LastName, string Email, ICollection<BookStructDto> Books);
                authors.Add(new AuthorRecordDto(reader.GetInt32(10), reader.GetString(11), reader.GetInt32(4), reader.GetString(3), reader.GetString(0), reader.GetString(1), reader.GetString(2),
                                                new[] { new BookRecordDto(reader.GetString(7), reader.GetInt32(8)) }));
            }

            return authors.GroupBy(x => x.UserName)
                          .Select(x => x.First() with
                           {
                               UserName = x.Key,
                               Books = x.SelectMany(y => y.Books).ToList()
                           })
                          .ToArray();
        }

        [Benchmark]
        public AuthorRecordDto[] GetAuthors_OptimizedMultipleQueries()
        {
            using var db = ContextFactory.CreateDbContext();

            var authors = db.Set<Author>()
                            .Where(AuthorWhereFilterExpression)
                            .Select(x => new
                             {
                                 x.Id,
                                 x.BooksCount,
                                 x.UserId
                             })
                            .OrderByDescending(x => x.BooksCount)
                            .Take(2)
                            .ToList();

            var books = db.Set<Book>()
                          .Where(BookWhereFilterExpression)
                          .Where(x => authors.Select(y => y.Id).Contains(x.AuthorId))
                          .Select(x => new
                           {
                               x.AuthorId,
                               x.Name,
                               x.Published.Year
                           })
                          .ToList();

            var users = db.Set<User>()
                          .Where(x => authors.Select(y => y.UserId).Contains(x.Id))
                          .Select(x => new
                           {
                               x.Id,
                               x.UserName,
                               x.Email,
                               x.FirstName,
                               x.LastName
                           })
                          .ToList();

            return authors.Select(x =>
                           {
                               var user = users.First(y => y.Id == x.UserId);

                               return new AuthorRecordDto(Age, Serbia, x.BooksCount, user.UserName, user.FirstName, user.LastName, user.Email,
                                                          books.Where(y => y.AuthorId == x.Id).Select(y => new BookRecordDto(y.Name, y.Year)).ToArray());
                           })
                          .ToArray();
        }

        [Benchmark]
        public AuthorRecordDto[] GetAuthors_MultipleQuery()
        {
            using var db = ContextFactory.CreateDbContext();

            var authors = GetAuthorsSimplified(db).ToList();
            var books = GetBooksSimplified(db, authors.Select(x => x.Id)).ToList();
            var users = GetUsersSimplified(db, authors.Select(x => x.UserId)).ToList();

            return ProcessMultipleQueryResult(authors, users, books);
        }

        [Benchmark]
        public AuthorRecordDto[] GetAuthors_MultipleQueryWithoutSelectorExpressions()
        {
            using var db = ContextFactory.CreateDbContext();

            var authors = GetAuthorsSimplifiedWithoutSelectorExpression(db).ToList();
            var books = GetBooksSimplifiedWithoutSelectorExpression(db, authors.Select(x => x.Id)).ToList();
            var users = GetUsersSimplifiedWithoutSelectorExpression(db, authors.Select(x => x.UserId)).ToList();

            return ProcessMultipleQueryResult(authors, users, books);
        }

        private static AuthorRecordDto[] ProcessMultipleQueryResult(IEnumerable<AuthorSimplified> authors, IReadOnlyCollection<UserSimplified> users, IReadOnlyCollection<BookSimplified> books)
        {
            return authors.Select(x =>
                           {
                               var user = users.First(y => y.Id == x.UserId);

                               return new AuthorRecordDto(Age, Serbia, x.BookCount, user.UserName, user.FirstName, user.LastName, user.Email,
                                                          books.Where(y => y.AuthorId == x.Id).Select(y => new BookRecordDto(y.Name, y.Year)).ToArray());
                           })
                          .ToArray();
        }

        private static readonly Func<DbContext, IEnumerable<AuthorSimplified>> GetAuthorsSimplifiedWithoutSelectorExpression =
            EF.CompileQuery((DbContext db) =>
                                db.Set<Author>()
                                  .Where(AuthorWhereFilterExpression)
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(author => new AuthorSimplified
                                   {
                                       Id = author.Id,
                                       BookCount = author.BooksCount,
                                       UserId = author.UserId
                                   }));

        private static readonly Func<DbContext, IEnumerable<int>, IEnumerable<BookSimplified>> GetBooksSimplifiedWithoutSelectorExpression =
            EF.CompileQuery((DbContext db, IEnumerable<int> authorIds) =>
                                db.Set<Book>()
                                  .Where(BookWhereFilterExpression)
                                  .Where(x => authorIds.Contains(x.AuthorId))
                                  .Select(book => new BookSimplified
                                   {
                                       AuthorId = book.AuthorId,
                                       Name = book.Name,
                                       Year = book.Published.Year
                                   }));

        private static readonly Func<DbContext, IEnumerable<int>, IEnumerable<UserSimplified>> GetUsersSimplifiedWithoutSelectorExpression =
            EF.CompileQuery((DbContext db, IEnumerable<int> userIds) =>
                                db.Set<User>()
                                  .Where(x => userIds.Contains(x.Id))
                                  .Select(user => new UserSimplified
                                   {
                                       Id = user.Id,
                                       UserName = user.UserName,
                                       Email = user.Email,
                                       FirstName = user.FirstName,
                                       LastName = user.LastName
                                   }));

        private static readonly Expression<Func<Author, AuthorSimplified>> AuthorSimplifiedSelectorExpression = author => new AuthorSimplified
        {
            Id = author.Id,
            BookCount = author.BooksCount,
            UserId = author.UserId
        };

        private static readonly Func<DbContext, IEnumerable<AuthorSimplified>> GetAuthorsSimplified =
            EF.CompileQuery((DbContext db) =>
                                db.Set<Author>()
                                  .Where(AuthorWhereFilterExpression)
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(AuthorSimplifiedSelectorExpression));

        private static readonly Expression<Func<Book, BookSimplified>> BookSimplifiedSelectorExpression = book => new BookSimplified
        {
            AuthorId = book.AuthorId,
            Name = book.Name,
            Year = book.Published.Year
        };

        private static readonly Func<DbContext, IEnumerable<int>, IEnumerable<BookSimplified>> GetBooksSimplified =
            EF.CompileQuery((DbContext db, IEnumerable<int> authorIds) =>
                                db.Set<Book>()
                                  .Where(BookWhereFilterExpression)
                                  .Where(x => authorIds.Contains(x.AuthorId))
                                  .Select(BookSimplifiedSelectorExpression));

        private static readonly Expression<Func<User, UserSimplified>> UserSimplifiedSelectorExpression = user => new UserSimplified
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        private static readonly Func<DbContext, IEnumerable<int>, IEnumerable<UserSimplified>> GetUsersSimplified =
            EF.CompileQuery((DbContext db, IEnumerable<int> userIds) =>
                                db.Set<User>()
                                  .Where(x => userIds.Contains(x.Id))
                                  .Select(UserSimplifiedSelectorExpression));

        private const string CommandText =
            @"SELECT [u].[FirstName], [u].[LastName], [u].[Email], [u].[UserName], [t].[BooksCount], [t].[Id], [u].[Id], [t0].[Name], [t0].[PublishedYear], [t0].[Id], [t].[Age], [t].[Country]
FROM (
    SELECT TOP(@take) [a].[Id], [a].[Age], [a].[BooksCount], [a].[Country], [a].[UserId]
    FROM [Authors] AS [a]
    WHERE ([a].[Country] = @country) AND ([a].[Age] = @age)
    ORDER BY [a].[BooksCount] DESC
) AS [t]
INNER JOIN [Users] AS [u] ON [t].[UserId] = [u].[Id]
LEFT JOIN (
    SELECT [b].[Name], DATEPART(year, [b].[Published]) AS [PublishedYear], [b].[Id], [b].[AuthorId]
    FROM [Books] AS [b]
    WHERE [b].[Published] < DATEFROMPARTS(@year, 1, 1)
) AS [t0] ON [t].[Id] = [t0].[AuthorId]
ORDER BY [t].[BooksCount] DESC, [t].[Id], [u].[Id]";

        private const string CommandTextWithoutParameters =
            @"SELECT [u].[FirstName], [u].[LastName], [u].[Email], [u].[UserName], [t].[BooksCount], [t].[Id], [u].[Id], [t0].[Name], [t0].[PublishedYear], [t0].[Id], [t].[Age], [t].[Country]
FROM (
    SELECT TOP(2) [a].[Id], [a].[Age], [a].[BooksCount], [a].[Country], [a].[UserId]
    FROM [Authors] AS [a]
    WHERE ([a].[Country] = N'Serbia') AND ([a].[Age] = 27)
    ORDER BY [a].[BooksCount] DESC
) AS [t]
INNER JOIN [Users] AS [u] ON [t].[UserId] = [u].[Id]
LEFT JOIN (
    SELECT [b].[Name], DATEPART(year, [b].[Published]) AS [PublishedYear], [b].[Id], [b].[AuthorId]
    FROM [Books] AS [b]
    WHERE [b].[Published] < DATEFROMPARTS(1900, 1, 1)
) AS [t0] ON [t].[Id] = [t0].[AuthorId]
ORDER BY [t].[BooksCount] DESC, [t].[Id], [u].[Id]";

        private static readonly ArrayPool<AuthorRecordDto> ArrayPool = ArrayPool<AuthorRecordDto>.Shared;

        private const string Serbia = nameof(Serbia);
        private const int Age = 27;
        private const int Year = 1900;

        public const string ConnectionString =
            "Server=c0711p\\sqlstafl;Database=OptimizeMePlease;Trusted_Connection=True;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true;";

        private static readonly PooledDbContextFactory<DbContext> ContextFactory = new(new DbContextOptionsBuilder<DbContext>().UseModel(AppDbContextModel.Instance)
                                                                                                                               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                                                                                                                               .EnableThreadSafetyChecks(false)
                                                                                                                               .UseSqlServer(ConnectionString)
                                                                                                                               .Options);

        private static readonly Expression<Func<Author, AuthorDtoOptimized>> AuthorDtoSelectorExpression = author => new AuthorDtoOptimized
        {
            UserFirstName = author.User.FirstName,
            UserLastName = author.User.LastName,
            UserEmail = author.User.Email,
            UserName = author.User.UserName,
            BooksCount = author.BooksCount,
            Books = author.Books.AsQueryable()
                          .Where(BookWhereFilterExpression)
                          .Select(BookSelectorExpression)
                          .ToList(),
            Age = author.Age,
            Country = author.Country
        };

        private static readonly Expression<Func<Book, BookDtoOptimized>> BookSelectorExpression = book => new BookDtoOptimized
        {
            Name = book.Name,
            PublishedYear = book.Published.Year
        };

        private static readonly Expression<Func<Author, AuthorRecordDto>> AuthorRecordDtoSelectorExpression = author =>
            new AuthorRecordDto(author.Age,
                                author.Country,
                                author.BooksCount,
                                author.User.UserName,
                                author.User.FirstName,
                                author.User.LastName,
                                author.User.Email,
                                author.Books.AsQueryable()
                                      .Where(BookWhereFilterExpression)
                                      .Select(BookRecordSelectorExpression)
                                      .ToList());

        private static readonly Expression<Func<Book, BookRecordDto>> BookRecordSelectorExpression = book => new BookRecordDto(book.Name, book.Published.Year);

        private static readonly Expression<Func<Author, bool>> AuthorWhereFilterExpression = author => (author.Country == Serbia) && (author.Age == Age);
        private static readonly Expression<Func<Book, bool>> BookWhereFilterExpression = book => book.Published < EF.Functions.DateFromParts(Year, 1, 1);

        private static readonly Func<DbContext, IEnumerable<AuthorDtoOptimized>> Get =
            EF.CompileQuery((DbContext db) =>
                                db.Set<Author>()
                                  .Where(AuthorWhereFilterExpression)
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(AuthorDtoSelectorExpression));

        private static readonly Func<DbContext, IEnumerable<AuthorDtoOptimized>> GetWithoutExpressions =
            EF.CompileQuery((DbContext db) =>
                                db.Set<Author>()
                                  .Where(author => (author.Country == Serbia) && (author.Age == Age))
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(author => new AuthorDtoOptimized
                                   {
                                       UserFirstName = author.User.FirstName,
                                       UserLastName = author.User.LastName,
                                       UserEmail = author.User.Email,
                                       UserName = author.User.UserName,
                                       BooksCount = author.BooksCount,
                                       Books = author.Books.AsQueryable()
                                                     .Where(book => book.Published < EF.Functions.DateFromParts(Year, 1, 1))
                                                     .Select(y => new BookDtoOptimized
                                                      {
                                                          Name = y.Name,
                                                          PublishedYear = y.Published.Year
                                                      })
                                                     .ToList(),
                                       Age = author.Age,
                                       Country = author.Country
                                   }));

        private static readonly Func<DbContext, string, int, IEnumerable<AuthorDtoOptimized>> GetParameterized =
            EF.CompileQuery((DbContext db, string country, int age) =>
                                db.Set<Author>()
                                  .Where(x => (x.Country == country) && (x.Age == age))
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(AuthorDtoSelectorExpression));

        private static readonly Func<DbContext, IAsyncEnumerable<AuthorDtoOptimized>> GetAsync =
            EF.CompileAsyncQuery((DbContext db) =>
                                     db.Set<Author>()
                                       .Where(AuthorWhereFilterExpression)
                                       .OrderByDescending(x => x.BooksCount)
                                       .Take(2)
                                       .Select(AuthorDtoSelectorExpression));

        private static readonly Func<DbContext, IEnumerable<AuthorRecordDto>> GetRecord =
            EF.CompileQuery((DbContext db) =>
                                db.Set<Author>()
                                  .Where(AuthorWhereFilterExpression)
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(AuthorRecordDtoSelectorExpression));

        private static readonly Func<DbContext, IEnumerable<AuthorRecordDto>> GetRecordWithoutExpressions =
            EF.CompileQuery((DbContext db) =>
                                db.Set<Author>()
                                  .Where(author => (author.Country == Serbia) && (author.Age == Age))
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(author =>
                                              new AuthorRecordDto(author.Age,
                                                                  author.Country,
                                                                  author.BooksCount,
                                                                  author.User.UserName,
                                                                  author.User.FirstName,
                                                                  author.User.LastName,
                                                                  author.User.Email,
                                                                  author.Books.AsQueryable()
                                                                        .Where(book => book.Published < EF.Functions.DateFromParts(Year, 1, 1))
                                                                        .Select(y => new BookRecordDto(y.Name, y.Published.Year))
                                                                        .ToList())));

        private static readonly Func<DbContext, IAsyncEnumerable<AuthorRecordDto>> GetRecordAsync =
            EF.CompileAsyncQuery((DbContext db) =>
                                     db.Set<Author>()
                                       .Where(AuthorWhereFilterExpression)
                                       .OrderByDescending(x => x.BooksCount)
                                       .Take(2)
                                       .Select(AuthorRecordDtoSelectorExpression));

        private static readonly Func<DbContext, string, int, IEnumerable<AuthorRecordDto>> GetRecordParameterized =
            EF.CompileQuery((DbContext db, string country, int age) =>
                                db.Set<Author>()
                                  .Where(x => (x.Country == country) && (x.Age == age))
                                  .OrderByDescending(x => x.BooksCount)
                                  .Take(2)
                                  .Select(AuthorRecordDtoSelectorExpression));
    }
}
