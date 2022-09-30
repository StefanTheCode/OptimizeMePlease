using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;

namespace OptimizeMePlease
{
    public static class IndexedContextProvider
    {
        static IndexedContextProvider()
        {
            AppDbContext = new IndexedDbContext();
            AppDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            AppDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public static AppDbContext AppDbContext { get; }
    }
}
