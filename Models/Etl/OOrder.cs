using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class OOrder
{
    public int Id { get; set; }
    [MaxLength(64)] public string OrderId { get; set; } = "";
    [MaxLength(64)] public string CustomerId { get; set; } = "";
    [MaxLength(32)] public string Status { get; set; } = "";
    public DateTime? PurchaseTs { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? DeliveredCarrierDate { get; set; }
    public DateTime? DeliveredCustomerDate { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}

