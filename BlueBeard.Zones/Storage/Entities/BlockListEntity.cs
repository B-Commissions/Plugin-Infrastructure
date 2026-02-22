using BlueBeard.Database.Attributes;

namespace BlueBeard.Zones.Storage.Entities;

[Table("bb_zone_blocklists")]
public class BlockListEntity
{
    [PrimaryKey]
    [Column("name")]
    public string Name { get; set; }

    [Column("items_json")]
    [ColumnType("TEXT")]
    public string ItemsJson { get; set; }
}
