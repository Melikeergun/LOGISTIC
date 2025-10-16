namespace MLYSO.Web.Models.Twin;

public sealed class PackingJob
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public List<PackingJobItem> Items { get; set; } = new();
    public List<PackingJobContainer> Containers { get; set; } = new();
}

public sealed class PackingJobItem
{
    public int Id { get; set; }
    public int PackingJobId { get; set; }
    public PackingJob Job { get; set; } = default!;

    public int BoxTypeId { get; set; }
    public BoxType BoxType { get; set; } = default!;
    public int Quantity { get; set; }
}

public sealed class PackingJobContainer
{
    public int Id { get; set; }
    public int PackingJobId { get; set; }
    public PackingJob Job { get; set; } = default!;

    public int ContainerTypeId { get; set; }
    public ContainerType ContainerType { get; set; } = default!;
    public int Quantity { get; set; }
}
