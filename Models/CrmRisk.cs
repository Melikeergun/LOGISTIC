namespace MLYSO.Web.Models;

public class CrmRisk
{
    public int Id { get; set; }
    public string Customer { get; set; } = "";
    public string Segment { get; set; } = "";   // B2B / B2C
    public int Risk { get; set; }               // 0..100
    public string? Note { get; set; }
}

