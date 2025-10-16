using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models.Twin;

public sealed class BoxType
{
    public int Id { get; set; }

    [MaxLength(24)]
    public string Code { get; set; } = "B-600x400x200";

    [MaxLength(64)]
    public string Name { get; set; } = "EuroMod 600x400x200";

    // mm
    public int L { get; set; }
    public int W { get; set; }
    public int H { get; set; }

    public double AvgWeightKg { get; set; } = 5;

    public bool AllowRotateX { get; set; } = true;
    public bool AllowRotateY { get; set; } = true;
    public bool AllowRotateZ { get; set; } = true;
}
