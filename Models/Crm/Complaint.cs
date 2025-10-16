using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class Complaint
{
    public int Id { get; set; }
    public int CrmCustomerId { get; set; }
    [MaxLength(32)] public string Status { get; set; } = "open"; // open/closed
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public string? Topic { get; set; }
}

