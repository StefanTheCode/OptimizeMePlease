using Microsoft.EntityFrameworkCore;

namespace OptimizeMePlease.Context
{
    public class IndexedDbContext : AppDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer("Server=localhost;Database=OptimizeMePlease-Indexed;Trusted_Connection=True;Integrated Security=true;MultipleActiveResultSets=true;Encrypt=false");
        }
    }
}