using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeMePlease.Entities
{
    public class UserRole
    {
        public int Id { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        public int UserId { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; }
        public int RoleId { get; set; }
    }
}