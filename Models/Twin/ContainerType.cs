using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models.Twin;

public sealed class ContainerType
{
    public int Id { get; set; }

    [MaxLength(24)]
    public string Code { get; set; } = "40HC";

    [MaxLength(64)]
    public string Name { get; set; } = "40' High Cube";

    public TransportMode Mode { get; set; } = TransportMode.Sea;

    // İç kullanılabilir ölçüler (mm)
    public int InnerL { get; set; }   // length
    public int InnerW { get; set; }   // width
    public int InnerH { get; set; }   // height

    // Yük kısıtı
    public double MaxPayloadKg { get; set; }

    // Havayolu ULD için işaret
    public bool IsULD { get; set; } = false;
}
