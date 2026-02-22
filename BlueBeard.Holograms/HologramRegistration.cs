using System.Collections.Generic;

namespace BlueBeard.Holograms;

public class HologramRegistration
{
    public IHologramDisplay Display { get; set; }
    public List<Hologram> Holograms { get; set; }
    public List<HologramDefinition> Definitions { get; set; }
    public bool IsGlobal { get; set; }
}
