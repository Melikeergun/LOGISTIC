using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class Product
{
    public int Id { get; set; }
    [MaxLength(64)] public string Sku { get; set; } = "";
    [MaxLength(128)] public string Name { get; set; } = "";
    public string? Unit { get; set; } = "pcs";
}

