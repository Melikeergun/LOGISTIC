namespace MLYSO.Web.Models;

public class Purchase
{
    public int Id { get; set; }
    public string Supplier { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "Planned"; 
}

