using System;
using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models.Twin;

public sealed class Warehouse
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Code { get; set; } = $"WH-{DateTime.UtcNow:yyyyMMddHHmmss}";

    [MaxLength(120)]
    public string Name { get; set; } = "Dijital İkiz Depo";

    // Depo boyutları (mm)
    public int LengthMm { get; set; } = 40000;
    public int WidthMm { get; set; } = 20000;
    public int HeightMm { get; set; } = 10000;

    public TransportMode Mode { get; set; } = TransportMode.Sea;
}
