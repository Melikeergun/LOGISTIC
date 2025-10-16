using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime UtcTs { get; set; } = DateTime.UtcNow;

        [MaxLength(256)] public string? UserName { get; set; }
        [MaxLength(64)] public string? UserRole { get; set; }

        [MaxLength(16)] public string Method { get; set; } = "";
        [MaxLength(512)] public string Path { get; set; } = "";
        public int StatusCode { get; set; }
        public int DurationMs { get; set; }

        [MaxLength(64)] public string? Ip { get; set; }
        [MaxLength(512)] public string? UserAgent { get; set; }
        [MaxLength(64)] public string? CorrelationId { get; set; }

        // Güvenli/limitli body kaydý
        [MaxLength(4096)] public string? RequestBody { get; set; }

        public bool IsError { get; set; }
        [MaxLength(1024)] public string? Error { get; set; }
    }
}

