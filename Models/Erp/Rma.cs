using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class Rma
{
    public int Id { get; set; }
    [MaxLength(32)] public string RmaNo { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public int? PurchaseOrderId { get; set; }
    public int? DynamicOrderId { get; set; } // müþteri iadesi ile bað
    [MaxLength(64)] public string Reason { get; set; } = "";
    [MaxLength(32)] public string Status { get; set; } = "open"; // open/inspected/recovered/disposed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

