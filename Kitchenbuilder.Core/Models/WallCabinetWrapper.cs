namespace Kitchenbuilder.Core.Models
{
    public class WallCabinetWrapper
    {
        public List<CabinetInfo> Cabinets { get; set; } = new();
        public List<Space> Spaces { get; set; } = new();
    }
}
