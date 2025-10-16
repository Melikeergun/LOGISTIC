using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class OSeller
{
    public int Id { get; set; }
    [MaxLength(64)] public string SellerId { get; set; } = "";
    [MaxLength(64)] public string ZipCode { get; set; } = "";
    [MaxLength(64)] public string City { get; set; } = "";
    [MaxLength(2)] public string State { get; set; } = "";
}

