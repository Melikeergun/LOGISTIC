namespace MLYSO.Web.Models;
public class Sla
{
    public int Id { get; set; }
    public int CrmCustomerId { get; set; }
    public int TargetHours { get; set; } = 24;
    public int BreachCount { get; set; } = 0;
}

