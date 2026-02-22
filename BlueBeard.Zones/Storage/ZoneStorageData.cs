namespace BlueBeard.Zones.Storage;

public class ZoneStorageData
{
    public string Id { get; set; }
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float CenterZ { get; set; }
    public string ShapeType { get; set; }
    public string ShapeData { get; set; }
    public string FlagsJson { get; set; }
    public string MetadataJson { get; set; }
    public float? LowerHeight { get; set; }
    public float? UpperHeight { get; set; }
    public int Priority { get; set; }
}
