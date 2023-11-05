using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizeMePlease
{
    [MemoryDiagnoser]
    [HideColumns(BenchmarkDotNet.Columns.Column.Job, BenchmarkDotNet.Columns.Column.RatioSD, BenchmarkDotNet.Columns.Column.StdDev, BenchmarkDotNet.Columns.Column.AllocRatio)]
    [Config(typeof(Config))]
    public class BenchmarkService
    {
        public BenchmarkService()
        {
        }

        private class Config : ManualConfig
        {
            public Config()
            {
                SummaryStyle = BenchmarkDotNet.Reports.SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
            }
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

        // 1260x faster than GetAuthors() with this query + these indexes
        // CREATE NONCLUSTERED INDEX idx_Books ON Books (AuthorId, Published) INCLUDE (Name)
        // CREATE NONCLUSTERED INDEX idx_Author ON Authors (Age, BooksCount DESC) INCLUDE (Id, Country, UserId)
        private static readonly Func<AppDbContext, IEnumerable<AuthorDTO>> CompiledOptimized =
            EF.CompileQuery((AppDbContext context) => context.Authors
                .Where(x => x.Country == "Serbia" && x.Age == 27)
                .OrderByDescending(x => x.BooksCount)
                .Select(x => new AuthorDTO
                {
                    UserFirstName = x.User.FirstName,
                    UserLastName = x.User.LastName,
                    UserEmail = x.User.Email,
                    UserName = x.User.UserName,
                    AuthorAge = x.Age,
                    AuthorCountry = x.Country,
                    AllBooks = x.Books.Where(b => b.Published.Year < 1900).Select(y => new BookDto
                    {
                        Name = y.Name,
                        Published = y.Published,
                    }).ToList()
                })
                .Take(2));
        
        /// <summary>
        /// Get top 2 Authors (FirstName, LastName, UserName, Email, Age, Country) 
        /// from country Serbia aged 27, with the highest BooksCount
        /// and all his/her books (Book Name/Title and Publishment Year) published before 1900
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized()
        {
            using var dbContext = new AppDbContext();

            var authors = CompiledOptimized(dbContext).ToList();

            return authors;
        }

        //[Benchmark]
        //public List<AuthorDTO_Optimized> GetAuthors_Optimized()
        //{
        //    using var dbContext = new AppDbContext();

        //    var date = new DateTime(1900, 1, 1);

        //    return dbContext.Authors
        //        .IncludeOptimized(x => x.Books.Where(b => b.Published < date))
        //        .AsNoTracking()
        //        .Where(x => x.Country == "Serbia" && x.Age == 27)
        //        .OrderByDescending(x => x.BooksCount)
        //        .Take(2)
        //        .Select(x => new AuthorDTO_Optimized
        //        {
        //            FirstName = x.User.FirstName,
        //            LastName = x.User.LastName,
        //            Email = x.User.Email,
        //            UserName = x.User.UserName,
        //            Books = x.Books.Select(y => new BookDTO_Optimized
        //            {
        //                Title = y.Name,
        //                PublishedYear = y.Published.Year
        //            }),
        //            Age = x.Age,
        //            Country = x.Country,
        //        })
        //        .ToList();
        //}

        //[Benchmark]
        public List<AuthorDTO_OptimizedStruct> GetAuthors_Optimized_Struct()
        {
            using var dbContext = new AppDbContext();

            var date = new DateTime(1900, 1, 1);

            return dbContext.Authors
                //.IncludeOptimized(x => x.Books)
                .Where(x => x.Country == "Serbia" && x.Age == 27)
                .OrderByDescending(x => x.BooksCount)
                .Select(x => new AuthorDTO_OptimizedStruct
                {
                    FirstName = x.User.FirstName,
                    LastName = x.User.LastName,
                    Email = x.User.Email,
                    UserName = x.User.UserName,
                    Books = x.Books.Where(b => b.Published.Year < 1900).Select(y => new BookDTO_OptimizedStruct
                    {
                        Title = y.Name,
                        PublishedYear = y.Published.Year
                    }),
                    Age = x.Age,
                    Country = x.Country,
                })
                .Take(2)
                .ToList();
        }

        //[Benchmark]
        public List<AuthorDTO_OptimizedStruct> GetAuthors_Optimized_Struct1()
        {
            using var dbContext = new AppDbContext();

            return dbContext.Authors
                .Where(x => x.Country == "Serbia" && x.Age == 27 && x.Books.Any(y => y.Published.Year < 1900))
                //.IncludeOptimized(x => x.Books.Where(y => y.Published.Year < 1900))
                .OrderByDescending(x => x.BooksCount)
                .Select(x => new AuthorDTO_OptimizedStruct
                {
                    FirstName = x.User.FirstName,
                    LastName = x.User.LastName,
                    Email = x.User.Email,
                    UserName = x.User.UserName,
                    Books = x.Books.Select(y => new BookDTO_OptimizedStruct
                    {
                        Title = y.Name,
                        PublishedYear = y.Published.Year
                    }),
                    Age = x.Age,
                    Country = x.Country,
                })
                .Take(2)
                .ToList();
        }
    }
}