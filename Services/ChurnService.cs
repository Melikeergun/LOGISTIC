using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

namespace MLYSO.Web.Services;

public class ChurnService
{
    private readonly AppDbContext _db;
    public ChurnService(AppDbContext db) { _db = db; }

    public async Task<List<(CrmCustomer cust, double score)>> ComputeAsync()
    {
        var result = new List<(CrmCustomer, double)>();
        var customers = await _db.CrmCustomers.ToListAsync();
        foreach (var c in customers)
        {
            double score = 0;
            var lastOrder = await _db.OOrders
                .Where(o => o.CustomerId == c.ExternalCustomerId && o.PurchaseTs != null)
                .OrderByDescending(o => o.PurchaseTs).FirstOrDefaultAsync();
            if (lastOrder == null || (DateTime.UtcNow - lastOrder.PurchaseTs!.Value).TotalDays > 90)
                score += 30;

            var openComplaints = await _db.Complaints.CountAsync(x => x.CrmCustomerId == c.Id && x.Status == "open");
            score += Math.Min(40, openComplaints * 20);

            var surveyAvg = await _db.Surveys.Where(s => s.CrmCustomerId == c.Id).Select(s => s.Score).DefaultIfEmpty(10).AverageAsync();
            if (surveyAvg < 6) score += 30;

            result.Add((c, Math.Min(100, score)));
        }
        return result.OrderByDescending(x => x.Item2).ToList();
    }
}
