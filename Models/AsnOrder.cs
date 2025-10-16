namespace MLYSO.Web.Models;

public class AsnOrder
{
    public int Id { get; set; }
    public DateTime Slot { get; set; }          // Randevu zamaný
    public string Sku { get; set; } = "";
    public int Qty { get; set; }
    public string Status { get; set; } = "Planned"; // Planned/Confirmed/Received...
}

