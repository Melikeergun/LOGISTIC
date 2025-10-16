namespace MLYSO.Web.Models;
public class PurchaseOrder
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "created"; // created/approved/received/invoiced
    public List<PurchaseOrderLine> Lines { get; set; } = new();
}
public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

