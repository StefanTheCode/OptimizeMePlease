using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;

namespace OptimizeMePlease
{

	/// <summary>
	/// Steps: 
	/// 
	/// 1. Create a database with name "OptimizeMePlease"
	/// 2. Run application Debug/Release mode for the first time. IWillPopulateData method will get the script and populate
	/// created db.
	/// 3. Comment or delete IWillPopulateData() call from Main method.
	/// 4. Go to BenchmarkService.cs class
	/// 5. Start coding within GetAuthors_Optimized method
	/// GOOD LUCK! :D
	/// </summary>
	public class Program
	{
		public const string ConnectionString = @"Filename=C:\Users\narta\source\repos\OptimizeMePlease\database.sqlite";

		static void Main(string[] args)
		{
#if DEBUG
			CreateDb();
			var benchmarkService = new BenchmarkService();

			benchmarkService.GlobalSetup();
			benchmarkService.GetAuthors();
			benchmarkService.GetAuthorsOptimized();
			benchmarkService.GetAuthorsOptimized_CompiledExpressions();
			benchmarkService.GlobalCleanup();
#else
			BenchmarkDotNet.Running.BenchmarkRunner.Run<BenchmarkService>();
			Console.ReadLine();
#endif
		}

		public static void CreateDb()
		{
			var dbContext = new AppDbContext();

			dbContext.Database.EnsureCreated();
			Console.WriteLine("Database is created");

			if (dbContext.Books.Any())
			{
				return;
			}

			var authorsSql = File.ReadAllText(Path.Combine("insert_scripts", "authors.sql"));
			var booksSql = File.ReadAllText(Path.Combine("insert_scripts", "books.sql"));
			var publishersSql = File.ReadAllText(Path.Combine("insert_scripts", "publishers.sql"));
			var rolesSql = File.ReadAllText(Path.Combine("insert_scripts", "roles.sql"));
			var userrolesSql = File.ReadAllText(Path.Combine("insert_scripts", "userroles.sql"));
			var usersSql = File.ReadAllText(Path.Combine("insert_scripts", "users.sql"));

			dbContext.Database.ExecuteSqlRaw(usersSql);
			Console.WriteLine("Users - inserted");

			dbContext.Database.ExecuteSqlRaw(rolesSql);
			Console.WriteLine("Roles - inserted");

			dbContext.Database.ExecuteSqlRaw(userrolesSql);
			Console.WriteLine("UserRoles - inserted");

			dbContext.Database.ExecuteSqlRaw(publishersSql);
			Console.WriteLine("Publishers - inserted");

			dbContext.Database.ExecuteSqlRaw(authorsSql);
			Console.WriteLine("Authors - inserted");

			dbContext.Database.ExecuteSqlRaw(booksSql);
			Console.WriteLine("Books - inserted");
		}
	}
}
