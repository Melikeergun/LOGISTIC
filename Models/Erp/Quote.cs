using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class Quote
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    [MaxLength(32)] public string Status { get; set; } = "open"; // open/accepted/rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<QuoteLine> Lines { get; set; } = new();
}
public class QuoteLine
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public int LeadTimeDays { get; set; }
    public double QualityScore { get; set; } // 0-100
}

