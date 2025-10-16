namespace MLYSO.Web.Models;
public class Survey
{
    public int Id { get; set; }
    public int CrmCustomerId { get; set; }
    public int Score { get; set; } // 1-10
    public DateTime At { get; set; } = DateTime.UtcNow;
}

