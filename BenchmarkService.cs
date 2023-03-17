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
    //[Config(typeof(Config))]
    public class BenchmarkService
    {
        public BenchmarkService()
        {
        }

        //private class Config : ManualConfig
        //{
        //    public Config()
        //    {
        //        SummaryStyle = BenchmarkDotNet.Reports.SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
        //    }
        //}

        /// <summary>
        /// Get top 2 Authors (FirstName, LastName, UserName, Email, Age, Country) 
        /// from country Serbia aged 27, with the highest BooksCount
        /// and all his/her books (Book Name/Title and Publishment Year) published before 1900
        /// </summary>
        /// <returns></returns>
        [Benchmark]
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
        
        /// Get top 2 Authors (FirstName, LastName, UserName, Email, Age, Country) 
        /// from country Serbia aged 27, with the highest BooksCount
        /// and all his/her books (Book Name/Title and Publishment Year) published before 1900
        [Benchmark]
        public List<AuthorDto_Optimized> GetAuthors_Optimized_()
        {
            using var dbContext = new AppDbContext();
            
            var orderedAuthors = dbContext.Authors.AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.Country == "Serbia" && x.Age == 27)
                .OrderByDescending(x=>x.BooksCount)
                .Select(x => new AuthorDto_Optimized
                {
                    
                    Id = x.Id,
                    FirstName = x.User.FirstName,
                    LastName = x.User.LastName,
                    Email = x.User.Email,
                    UserName = x.User.UserName,
                    UserId = x.UserId,
                    Age = x.Age,
                    Country = x.Country
                })
                .Take(2).ToList();
         

            var userIdArray = orderedAuthors.Select(x => x.Id).ToArray();
            var dt = new DateTime(1900, 1, 1);
            var books = dbContext.Books.Where(x => userIdArray.Contains(x.AuthorId) && x.Published < dt).Select(x => new BookStruct()
            {
                AuthorId = x.AuthorId,
                Title = x.Name,
                PublishedYear = x.Published.Year
            }).ToList();
            
            foreach (var auth in orderedAuthors)
            {
                auth.Books = books.Where(b => b.AuthorId == auth.Id).ToList();
            }
            
            return orderedAuthors;
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

        [Benchmark]
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