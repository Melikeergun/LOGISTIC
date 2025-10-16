namespace MLYSO.Web.Models;
public class ChurnScore
{
    public int Id { get; set; }
    public int CrmCustomerId { get; set; }
    public double Score { get; set; } // 0-100 risk
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}

