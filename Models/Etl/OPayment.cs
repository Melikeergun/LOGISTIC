using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class OPayment
{
    public int Id { get; set; }
    [MaxLength(64)] public string OrderId { get; set; } = "";
    public int Seq { get; set; }
    [MaxLength(32)] public string PaymentType { get; set; } = "";
    public decimal Value { get; set; }
}

