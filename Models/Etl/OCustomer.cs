using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class OCustomer
{
    public int Id { get; set; }
    [MaxLength(64)] public string CustomerId { get; set; } = "";
    [MaxLength(64)] public string ZipCode { get; set; } = "";
    [MaxLength(64)] public string City { get; set; } = "";
    [MaxLength(8)] public string State { get; set; } = "";
}

