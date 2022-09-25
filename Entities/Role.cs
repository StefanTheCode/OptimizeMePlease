using System.Collections.Generic;

namespace OptimizeMePlease.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public virtual List<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public string Name { get; set; }
    }
}