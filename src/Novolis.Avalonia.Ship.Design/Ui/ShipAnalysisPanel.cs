using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Analysis;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>ANALYZE workspace findings + load case / breach controls.</summary>
public static class ShipAnalysisPanel
{
    public static Control Build(ShipDesignSession session)
    {
        var findings = new TextBlock
        {
            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.WhiteSmoke,
        };

        var loadCaseBox = new ComboBox { Width = 180 };
        var breachBox = new ComboBox { Width = 180 };
        var syncing = false;

        loadCaseBox.SelectionChanged += (_, _) =>
        {
            if (syncing)
                return;
            if (loadCaseBox.SelectedItem is string id)
                session.SetActiveLoadCase(id);
        };

        breachBox.SelectionChanged += (_, _) =>
        {
            if (syncing)
                return;
            if (breachBox.SelectedItem is BreachItem b)
                session.SetBreachCompartment(b.Id);
            else
                session.SetBreachCompartment(null);
        };

        void Rebuild()
        {
            syncing = true;
            try
            {
                loadCaseBox.ItemsSource = session.Design.LoadCases.Select(c => c.Id).ToList();
                loadCaseBox.SelectedItem = session.ActiveLoadCaseId
                    ?? session.Design.LoadCases.FirstOrDefault()?.Id;

                var breaches = new List<BreachItem> { new("(none)", null) };
                breaches.AddRange(session.Design.Compartments.Select(c => new BreachItem(c.Name, c.Id.Value)));
                breachBox.ItemsSource = breaches;
                breachBox.SelectedIndex = 0;
                if (session.BreachCompartmentId is { } bid)
                {
                    var idx = breaches.FindIndex(b => b.Id == bid);
                    if (idx >= 0)
                        breachBox.SelectedIndex = idx;
                }
            }
            finally
            {
                syncing = false;
            }

            var report = session.Analysis;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"ANALYZE · load {report.ActiveLoadCaseId}");
            sb.AppendLine($"Mass {report.TotalMassKg / 1000f:0.##} t · worst {report.Worst}");
            if (session.ActiveAnalysisCategory is { } cat)
            {
                sb.AppendLine();
                sb.AppendLine($"Overlay: {cat}");
                foreach (var f in report.Findings.Where(x => x.Category == cat).Take(20))
                    sb.AppendLine($"  [{f.Severity}] {f.Code}: {f.Message}");
            }
            else
            {
                sb.AppendLine();
                foreach (var f in report.Findings.Take(16))
                    sb.AppendLine($"  [{f.Severity}] {f.Category}/{f.Code}: {f.Message}");
            }

            if (report.DecompressionCascade.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Decompression cascade:");
                foreach (var name in report.DecompressionCascade.Take(12))
                    sb.AppendLine($"  · {name}");
            }

            findings.Text = sb.ToString();
        }

        session.Changed += Rebuild;
        Rebuild();

        var form = new StackPanel { Spacing = 6, Margin = new Thickness(8) };
        form.Children.Add(new TextBlock
        {
            Text = "ANALYZE",
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
        });
        form.Children.Add(Labeled("Load case", loadCaseBox));
        form.Children.Add(Labeled("Breach", breachBox));
        form.Children.Add(findings);
        return form;
    }

    private static Control Labeled(string label, Control control)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock
        {
            Text = label,
            Width = 80,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Gray,
        });
        row.Children.Add(control);
        return row;
    }

    private sealed record BreachItem(string Name, Guid? Id)
    {
        public override string ToString() => Name;
    }
}
