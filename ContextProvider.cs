﻿using Microsoft.EntityFrameworkCore;
using OptimizeMePlease.Context;

namespace OptimizeMePlease
{
    public static class ContextProvider
    {
        static ContextProvider()
        {
            AppDbContext = new AppDbContext();
            AppDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            AppDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public static AppDbContext AppDbContext { get; }
    }
}
