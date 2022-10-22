using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;

namespace OptimizeMePlease
{
	[MemoryDiagnoser]
	[HideColumns(Column.Job, Column.RatioSD, Column.StdDev, Column.AllocRatio)]
	[Config(typeof(Config))]
	public class BenchmarkService
	{
		private class Config : ManualConfig
		{
			public Config()
			{
				SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
			}
		}

		private AppDbContext dbContext;

		[GlobalSetup]
		public void GlobalSetup()
		{
			dbContext = new AppDbContext();
		}

		[GlobalCleanup]
		public void GlobalCleanup()
		{
			dbContext.Dispose();
		}

		/// <summary>
		/// Get top 2 Authors (FirstName, LastName, UserName, Email, Age, Country) 
		/// from country Serbia aged 27, with the highest BooksCount
		/// and all his/her books (Book Name/Title and Publishment Year) published before 1900
		/// </summary>
		[Benchmark(Baseline = true)]
		public List<AuthorDTO> GetAuthors()
		{
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
		public List<AuthorDTO> GetAuthorsOptimized()
		{
			var authors = dbContext.Authors
				.Include(author => author.User)
				.Include(author => author.Books.Where(book => book.Published.Year < 1900))
				.Where(author => author.Country == "Serbia" && author.Age == 27)
				.OrderByDescending(author => author.BooksCount)
				.Select(author => new AuthorDTO
				{
					UserFirstName = author.User.FirstName,
					UserLastName = author.User.LastName,
					UserEmail = author.User.Email,
					UserName = author.User.UserName,
					AuthorAge = author.Age,
					AuthorCountry = author.Country,
					AllBooks = author.Books.Select(book => new BookDto
					{
						Name = book.Name,
						Published = book.Published
					}).ToList(),
				})
				.Take(2)
				.ToList();

			return authors;
		}

		private static readonly Func<AppDbContext, IEnumerable<AuthorDTO>> CompiledQuery =
				EF.CompileQuery((AppDbContext context) => context.Authors
						.Include(author => author.User)
						.Include(author => author.Books.Where(book => book.Published.Year < 1900))
						.Where(author => author.Country == "Serbia" && author.Age == 27)
						.OrderByDescending(author => author.BooksCount)
						.Select(author => new AuthorDTO
						{
							UserFirstName = author.User.FirstName,
							UserLastName = author.User.LastName,
							UserEmail = author.User.Email,
							UserName = author.User.UserName,
							AuthorAge = author.Age,
							AuthorCountry = author.Country,
							AllBooks = author.Books.Select(book => new BookDto
							{
								Name = book.Name,
								Published = book.Published
							}).ToList(),
						})
						.Take(2));

		[Benchmark]
		public List<AuthorDTO> GetAuthorsOptimized_CompiledExpressions()
		{
			var authors = CompiledQuery(dbContext).ToList();
			return authors;
		}
	}
}