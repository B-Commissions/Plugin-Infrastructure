using BlueBeard.Database.Attributes;

namespace BlueBeard.Zones.Storage.Entities;

[Table("bb_zones")]
public class ZoneEntity
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; }

    [Column("center_x")]
    public float CenterX { get; set; }

    [Column("center_y")]
    public float CenterY { get; set; }

    [Column("center_z")]
    public float CenterZ { get; set; }

    [Column("shape_type")]
    public string ShapeType { get; set; }

    [Column("shape_data")]
    [ColumnType("TEXT")]
    public string ShapeData { get; set; }

    [Column("flags_json")]
    [ColumnType("TEXT")]
    public string FlagsJson { get; set; }

    [Column("metadata_json")]
    [ColumnType("TEXT")]
    public string MetadataJson { get; set; }

    [Column("lower_height")]
    [ColumnType("FLOAT NULL")]
    public float LowerHeight { get; set; }

    [Column("upper_height")]
    [ColumnType("FLOAT NULL")]
    public float UpperHeight { get; set; }

    [Column("priority")]
    public int Priority { get; set; }
}
