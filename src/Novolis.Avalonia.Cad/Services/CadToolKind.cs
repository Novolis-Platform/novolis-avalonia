using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;
using Novolis.Commands.Expressions;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Cad.Services;

public enum CadToolKind
{
    Select,
    Line,
    Circle,
    Rect,
    Spline,
    Wall,
    Dimension,
}
