using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;

public class Shipment
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string ShipNo { get; set; } = $"SHP-{DateTime.UtcNow:yyyyMMddHHmmss}";

    [MaxLength(16)]
    public string VehiclePlate { get; set; } = "";

    [MaxLength(24)]
    public string Status { get; set; } = "Planned"; // Planned, Dispatched, Delivered, Cancelled...

    public DateTime PlannedDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}
