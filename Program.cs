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
			using var dbContext = new AppDbContext();

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

			Console.Write("Inserting users");
			dbContext.Database.ExecuteSqlRaw(usersSql);
			Console.WriteLine(": done");

			Console.Write("Inserting roles");
			dbContext.Database.ExecuteSqlRaw(rolesSql);
			Console.WriteLine(": done");

			Console.Write("Inserting userroles");
			dbContext.Database.ExecuteSqlRaw(userrolesSql);
			Console.WriteLine(": done");

			Console.Write("Inserting publishers");
			dbContext.Database.ExecuteSqlRaw(publishersSql);
			Console.WriteLine(": done");

			Console.Write("Inserting authors");
			dbContext.Database.ExecuteSqlRaw(authorsSql);
			Console.WriteLine(": done");

			Console.Write("Inserting books");
			dbContext.Database.ExecuteSqlRaw(booksSql);
			Console.WriteLine(": done");
		}

		public static string GetConnectionString()
		{
			var currentFolder = new DirectoryInfo(Environment.CurrentDirectory);

			while (currentFolder.Name != "OptimizeMePlease")
			{
				currentFolder = currentFolder.Parent;
			}

			Console.WriteLine(Path.Combine(currentFolder.FullName, "database.sqlite"));

			return $"Filename={Path.Combine(currentFolder.FullName, "database.sqlite")}";
		}
	}
}
