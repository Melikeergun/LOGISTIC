namespace MLYSO.Web.Models;

public class Route
{
    public int Id { get; set; }
    public int DriverUserId { get; set; }
    public DateTime RouteDate { get; set; }
    public string Vehicle { get; set; } = "";
    public string Status { get; set; } = "Planned";
    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
}

public class RouteStop
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public int StopNo { get; set; }
    public string Customer { get; set; } = "";
    public string Address { get; set; } = "";
    public string OrderId { get; set; } = "";
    public DateTime PlannedTime { get; set; }
    public string Status { get; set; } = "Pending";

    // >>> AppDbContext.OnModelCreating bunun için konfig yapýyor
    public string? ProofCode { get; set; }
}
