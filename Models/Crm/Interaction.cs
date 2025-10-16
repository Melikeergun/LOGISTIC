using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class Interaction
{
    public int Id { get; set; }
    public int CrmCustomerId { get; set; }
    [MaxLength(32)] public string Type { get; set; } = "call"; // call/email/chat/visit
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}

