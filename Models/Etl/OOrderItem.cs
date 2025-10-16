using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class OOrderItem
{
    public int Id { get; set; }
    [MaxLength(64)] public string OrderId { get; set; } = "";
    public int ItemSeq { get; set; }
    [MaxLength(64)] public string ProductId { get; set; } = "";
    [MaxLength(64)] public string SellerId { get; set; } = "";
    [MaxLength(128)] public string ProductCategory { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Freight { get; set; }
}

