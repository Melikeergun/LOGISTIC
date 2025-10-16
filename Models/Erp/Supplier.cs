using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class Supplier
{
    public int Id { get; set; }
    [MaxLength(128)] public string Name { get; set; } = "";
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public double ReliabilityScore { get; set; } = 0; // 0-100
}

