using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;

namespace OptimizeMePlease
{
    // I use a static context to gain a few more ms, must not be used in code meant for production.
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
