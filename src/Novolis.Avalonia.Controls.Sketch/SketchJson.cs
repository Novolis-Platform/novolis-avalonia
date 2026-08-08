using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>JSON serialize/deserialize for <see cref="SketchDocument"/> (selection/history excluded).</summary>
public static class SketchJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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
        Kind = s.Kind == SketchElementKind.Stroke ? null : s.Kind,
        StrokeColor = s.StrokeColor,
        StrokeWidth = s.StrokeWidth,
        FillColor = string.IsNullOrWhiteSpace(s.FillColor) ? null : s.FillColor,
        StrokeStyle = s.StrokeStyle == SketchStrokeStyle.Solid ? null : s.StrokeStyle,
        Closed = s.Closed ? true : null,
        RotationDegrees = Math.Abs(s.RotationDegrees) < 1e-9 ? null : s.RotationDegrees,
        GroupId = string.IsNullOrWhiteSpace(s.GroupId) ? null : s.GroupId,
        Text = string.IsNullOrEmpty(s.Text) ? null : s.Text,
        FontSize = s.Kind is SketchElementKind.Text or SketchElementKind.TextBox && Math.Abs(s.FontSize - 16) > 1e-9
            ? s.FontSize
            : null,
        ImagePngBase64 = string.IsNullOrWhiteSpace(s.ImagePngBase64) ? null : s.ImagePngBase64,
        Points = s.Points.Select(p => new SketchPointDto { X = p.X, Y = p.Y }).ToList()
    };

    static StrokeShape FromDto(StrokeShapeDto d) => new()
    {
        Id = string.IsNullOrWhiteSpace(d.Id) ? Guid.NewGuid().ToString("N") : d.Id,
        Kind = d.Kind ?? SketchElementKind.Stroke,
        StrokeColor = string.IsNullOrWhiteSpace(d.StrokeColor) ? "#1e1e1e" : d.StrokeColor!,
        StrokeWidth = d.StrokeWidth <= 0 ? 2 : d.StrokeWidth,
        FillColor = string.IsNullOrWhiteSpace(d.FillColor) ? null : d.FillColor,
        StrokeStyle = d.StrokeStyle ?? SketchStrokeStyle.Solid,
        Closed = d.Closed == true,
        RotationDegrees = d.RotationDegrees ?? 0,
        GroupId = string.IsNullOrWhiteSpace(d.GroupId) ? null : d.GroupId,
        Text = d.Text,
        FontSize = d.FontSize is > 0 ? d.FontSize.Value : 16,
        ImagePngBase64 = string.IsNullOrWhiteSpace(d.ImagePngBase64) ? null : d.ImagePngBase64,
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
        public SketchElementKind? Kind { get; set; }
        public string? StrokeColor { get; set; }
        public double StrokeWidth { get; set; } = 2;
        public string? FillColor { get; set; }
        public SketchStrokeStyle? StrokeStyle { get; set; }
        public bool? Closed { get; set; }
        public double? RotationDegrees { get; set; }
        public string? GroupId { get; set; }
        public string? Text { get; set; }
        public double? FontSize { get; set; }
        public string? ImagePngBase64 { get; set; }
        public List<SketchPointDto>? Points { get; set; }
    }

    sealed class SketchPointDto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
