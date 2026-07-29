using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Avalonia.Controls;

/// <summary>JSON serialize/deserialize for <see cref="SketchDocument"/> (selection/history excluded).</summary>
public static class SketchJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Serializes document elements and grid settings to JSON.</summary>
    public static string Serialize(SketchDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var dto = new SketchDocumentDto
        {
            Version = document.Version,
            Grid = new GridSettingsDto
            {
                Size = document.Grid.Size,
                Visible = document.Grid.Visible,
                SnapEnabled = document.Grid.SnapEnabled
            },
            Elements = document.Elements.Select(ToDto).ToList()
        };
        return JsonSerializer.Serialize(dto, Options);
    }

    /// <summary>Deserializes a document from JSON (clears selection/history).</summary>
    public static SketchDocument Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var dto = JsonSerializer.Deserialize<SketchDocumentDto>(json, Options)
                  ?? throw new InvalidOperationException("Invalid sketch JSON.");
        var doc = new SketchDocument { Version = dto.Version <= 0 ? 1 : dto.Version };
        if (dto.Grid is not null)
        {
            doc.Grid.Size = dto.Grid.Size;
            doc.Grid.Visible = dto.Grid.Visible;
            doc.Grid.SnapEnabled = dto.Grid.SnapEnabled;
        }

        if (dto.Elements is { Count: > 0 })
        {
            doc.ReplaceElements(dto.Elements.Select(FromDto));
        }

        return doc;
    }

    static StrokeShapeDto ToDto(StrokeShape s) => new()
    {
        Id = s.Id,
        StrokeColor = s.StrokeColor,
        StrokeWidth = s.StrokeWidth,
        Points = s.Points.Select(p => new SketchPointDto { X = p.X, Y = p.Y }).ToList()
    };

    static StrokeShape FromDto(StrokeShapeDto d) => new()
    {
        Id = string.IsNullOrWhiteSpace(d.Id) ? Guid.NewGuid().ToString("N") : d.Id,
        StrokeColor = string.IsNullOrWhiteSpace(d.StrokeColor) ? "#1e1e1e" : d.StrokeColor!,
        StrokeWidth = d.StrokeWidth <= 0 ? 2 : d.StrokeWidth,
        Points = d.Points?.Select(p => new SketchPoint(p.X, p.Y)).ToList() ?? []
    };

    sealed class SketchDocumentDto
    {
        public int Version { get; set; } = 1;
        public GridSettingsDto? Grid { get; set; }
        public List<StrokeShapeDto>? Elements { get; set; }
    }

    sealed class GridSettingsDto
    {
        public double Size { get; set; } = 20;
        public bool Visible { get; set; } = true;
        public bool SnapEnabled { get; set; }
    }

    sealed class StrokeShapeDto
    {
        public string? Id { get; set; }
        public string? StrokeColor { get; set; }
        public double StrokeWidth { get; set; } = 2;
        public List<SketchPointDto>? Points { get; set; }
    }

    sealed class SketchPointDto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
