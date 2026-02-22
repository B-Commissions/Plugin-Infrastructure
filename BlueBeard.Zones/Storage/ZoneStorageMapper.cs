using System.Collections.Generic;
using System.Linq;
using BlueBeard.Zones.Shapes;
using Newtonsoft.Json;
using UnityEngine;

namespace BlueBeard.Zones.Storage;

public static class ZoneStorageMapper
{
    public static ZoneStorageData ToStorageData(ZoneDefinition definition)
    {
        return new ZoneStorageData
        {
            Id = definition.Id,
            CenterX = definition.Center.x,
            CenterY = definition.Center.y,
            CenterZ = definition.Center.z,
            ShapeType = definition.Shape.ShapeType,
            ShapeData = SerializeShape(definition.Shape),
            FlagsJson = definition.Flags != null && definition.Flags.Count > 0
                ? JsonConvert.SerializeObject(definition.Flags)
                : null,
            MetadataJson = definition.Metadata != null && definition.Metadata.Count > 0
                ? JsonConvert.SerializeObject(definition.Metadata)
                : null,
            LowerHeight = definition.LowerHeight,
            UpperHeight = definition.UpperHeight,
            Priority = definition.Priority
        };
    }

    public static ZoneDefinition ToDefinition(ZoneStorageData data)
    {
        return new ZoneDefinition
        {
            Id = data.Id,
            Center = new Vector3(data.CenterX, data.CenterY, data.CenterZ),
            Shape = DeserializeShape(data.ShapeType, data.ShapeData),
            Flags = !string.IsNullOrEmpty(data.FlagsJson)
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(data.FlagsJson)
                : new Dictionary<string, string>(),
            Metadata = !string.IsNullOrEmpty(data.MetadataJson)
                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(data.MetadataJson)
                : null,
            LowerHeight = data.LowerHeight,
            UpperHeight = data.UpperHeight,
            Priority = data.Priority
        };
    }

    private static string SerializeShape(IZoneShape shape)
    {
        return shape switch
        {
            RadiusZoneShape radius => JsonConvert.SerializeObject(new
            {
                radius = radius.Radius,
                height = radius.Height
            }),
            PolygonZoneShape polygon => JsonConvert.SerializeObject(new
            {
                height = polygon.Height,
                points = polygon.WorldPoints.Select(p => new { x = p.x, y = p.y, z = p.z }).ToArray()
            }),
            _ => "{}"
        };
    }

    private static IZoneShape DeserializeShape(string shapeType, string shapeData)
    {
        switch (shapeType)
        {
            case "radius":
            {
                var data = JsonConvert.DeserializeAnonymousType(shapeData, new { radius = 0f, height = 0f });
                return new RadiusZoneShape(data.radius, data.height);
            }
            case "polygon":
            {
                var data = JsonConvert.DeserializeAnonymousType(shapeData, new
                {
                    height = 0f,
                    points = new[] { new { x = 0f, y = 0f, z = 0f } }
                });
                var points = data.points.Select(p => new Vector3(p.x, p.y, p.z)).ToArray();
                return new PolygonZoneShape(points, data.height);
            }
            default:
                return new RadiusZoneShape(10f, 30f);
        }
    }
}
