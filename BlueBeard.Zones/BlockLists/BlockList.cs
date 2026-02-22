using System.Collections.Generic;

namespace BlueBeard.Zones.BlockLists;

public class BlockList
{
    public string Name { get; set; }
    public HashSet<ushort> Items { get; set; } = new();
}
