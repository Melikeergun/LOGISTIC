using System.ComponentModel.DataAnnotations.Schema;

namespace MLYSO.Web.Models.Twin;

public sealed class PackingPlan
{
    public int Id { get; set; }
    public int PackingJobId { get; set; }
    public PackingJob Job { get; set; } = default!;

    public double VolumeUtilizationPct { get; set; }
    public double WeightUtilizationPct { get; set; }
    public int ContainersUsed { get; set; }

    public List<WarehousePlacement> WarehousePlacements { get; set; } = new();
    [NotMapped]
    public List<string> Notes { get; } = new();

}

public sealed class WarehousePlacement
{
    public int Id { get; set; }
    public int PackingPlanId { get; set; }
    public PackingPlan Plan { get; set; } = default!;

    public int ContainerTypeId { get; set; }
    public ContainerType ContainerType { get; set; } = default!;

    // Depo kat planı (mm)
    public int X { get; set; }
    public int Y { get; set; }
    public int RotDeg { get; set; } // 0/90

    public List<BoxPlacement> BoxPlacements { get; set; } = new();
}

public sealed class BoxPlacement
{
    public int Id { get; set; }
    public int WarehousePlacementId { get; set; }
    public WarehousePlacement WarehousePlacement { get; set; } = default!;

    public int BoxTypeId { get; set; }
    public BoxType BoxType { get; set; } = default!;

    // Konteyner içi 3D pozisyon/rotasyon
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public int RotX { get; set; }
    public int RotY { get; set; }
    public int RotZ { get; set; }
}
