using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class CrmCustomer
{
    public int Id { get; set; }
    [MaxLength(64)] public string ExternalCustomerId { get; set; } = ""; // olist customer_id eþlemesi
    [MaxLength(128)] public string Name { get; set; } = "";
    [MaxLength(128)] public string Segment { get; set; } = "retail";
}

