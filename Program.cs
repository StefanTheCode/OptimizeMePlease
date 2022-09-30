using BenchmarkDotNet.Running;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.IO;

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
            //Debugging 
            //BenchmarkService benchmarkService = new BenchmarkService();
            // Original query
            //var nonOpt = benchmarkService.GetAuthors();
            // Optimized query without uchanging the dto
            //var Opt = benchmarkService.GetAuthors_Optimized();
            // Optimized query with changing DTOs
            //var Opt_Meaningful = benchmarkService.GetAuthors_Optimized_Meaningful();

            //Comment me after first execution, please.
            //IWillPopulateData();

            BenchmarkRunner.Run<BenchmarkService>();
        }

        public static void IWillPopulateData()
        {
            string sqlConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;Database=OptimizeMePlease";

            string workingDirectory = Environment.CurrentDirectory;
            string path = Path.Combine(Directory.GetParent(workingDirectory).Parent.Parent.FullName, @"script.sql");
            string script = File.ReadAllText(path);

            SqlConnection conn = new SqlConnection(sqlConnectionString);

            Server server = new Server(new ServerConnection(conn));

            server.ConnectionContext.ExecuteNonQuery(script);
        }
    }
}
