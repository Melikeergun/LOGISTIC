using System;
using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? FullName { get; set; }

        [MaxLength(128)]
        public string? Email { get; set; }

        [Required, MaxLength(512)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? Role { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(64)] public string? SelectedRole { get; set; }
        public bool RoleChosen { get; set; }
        public bool IsAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
