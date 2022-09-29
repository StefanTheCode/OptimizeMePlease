using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;
using System.Collections.Generic;
using System.Linq;

namespace OptimizeMePlease
{
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

        [Benchmark]
        public List<AuthorDTO> GetAuthors_Optimized()
        {
            using var dbContext = new AppDbContext();
            var authors = dbContext.Authors
                                        .AsNoTracking()
                                        .Include(x => x.User)
                                        .Include(x => x.Books)
                                        .ThenInclude(x => x.Publisher)
                                        .Where(x => x.Country == "Serbia" && x.Age == 27)
                                        .OrderByDescending(x => x.BooksCount)
                                        .Select(x => new AuthorDTO
                                        {
                                            UserFirstName = x.User.FirstName,
                                            UserLastName = x.User.LastName,
                                            UserName = x.User.UserName,
                                            UserEmail = x.User.Email,
                                            AuthorAge = x.Age,
                                            AuthorCountry = x.Country,
                                            AllBooks = x.Books.Where(b => b.Published.Year < 1900).Select(y => new BookDto
                                            {
                                                Name = y.Name,
                                                PublishedYear = y.Published.Year
                                            }).ToList(),
                                        }).Take(2).ToList();
            
            return authors;
        }
    }
}
