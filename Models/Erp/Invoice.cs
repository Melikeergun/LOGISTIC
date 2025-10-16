namespace MLYSO.Web.Models;
public class Invoice
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public string InvoiceNo { get; set; } = "";
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
}

