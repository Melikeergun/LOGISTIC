namespace MLYSO.Web.Models;

public class WhTask
{
    public int Id { get; set; }
    public string Type { get; set; } = "";      // Pick / Putaway / Count
    public string Status { get; set; } = "Open";
    public string Location { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

