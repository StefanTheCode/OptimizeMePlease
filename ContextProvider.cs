using OptimizeMePlease.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizeMePlease
{
    public static class ContextProvider
    {
        static ContextProvider()
        {
            AppDbContext = new AppDbContext();
            AppDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            AppDbContext.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
        }

        public static AppDbContext AppDbContext { get; }
    }
}
