using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Analysis;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>Compact live GREEN/YELLOW/RED analysis chips (§28).</summary>
public static class ShipAnalysisStatusStrip
{
    public static Control Build(ShipDesignSession session)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 2),
        };

        void Rebuild()
        {
            row.Children.Clear();
            foreach (var cat in new[]
                     {
                         AnalysisCategory.Mass,
                         AnalysisCategory.Cg,
                         AnalysisCategory.Pressure,
                         AnalysisCategory.Structure,
                         AnalysisCategory.Clearance,
                     })
            {
                var status = session.Analysis.Categories.FirstOrDefault(c => c.Category == cat);
                var severity = status?.Severity ?? AnalysisSeverity.Green;
                var count = status?.FindingCount ?? 0;
                var label = count > 0 ? $"{cat}  {severity.ToString().ToUpperInvariant()} {count}" : $"{cat}  {severity.ToString().ToUpperInvariant()}";
                var btn = new Button
                {
                    Content = label,
                    Padding = new Thickness(8, 3),
                    Background = BrushFor(severity),
                    Foreground = Brushes.White,
                    FontSize = 11,
                };
                var captured = cat;
                btn.Click += (_, _) => session.SetAnalysisCategory(captured);
                row.Children.Add(btn);
            }

            row.Children.Add(new TextBlock
            {
                Text = $"mass {session.Analysis.TotalMassKg / 1000f:0.#} t · CG ({session.Analysis.CenterOfMassX:0.#},{session.Analysis.CenterOfMassY:0.#},{session.Analysis.CenterOfMassZ:0.#})",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                Foreground = Brushes.LightGray,
                FontSize = 11,
            });
        }

        session.Changed += Rebuild;
        Rebuild();
        return row;
    }

    private static IBrush BrushFor(AnalysisSeverity severity) => severity switch
    {
        AnalysisSeverity.Red => new SolidColorBrush(Color.Parse("#8b2e2e")),
        AnalysisSeverity.Yellow => new SolidColorBrush(Color.Parse("#8a6a1a")),
        _ => new SolidColorBrush(Color.Parse("#1f5c3a")),
    };
}
