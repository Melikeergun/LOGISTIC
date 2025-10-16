using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;
public class OGeo
{
    public int Id { get; set; }
    [MaxLength(64)] public string ZipCode { get; set; } = "";
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

