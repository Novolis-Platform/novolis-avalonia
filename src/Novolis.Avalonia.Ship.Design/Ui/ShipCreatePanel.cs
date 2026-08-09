using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>Non-modal create-ship panel (definition → immediately valid design).</summary>
public static class ShipCreatePanel
{
    public static Control Build(ShipDesignSession session, Action? onCreated = null)
    {
        var nameBox = new TextBox { Text = "New Ship", PlaceholderText = "Name" };
        var lengthBox = Num(90m);
        var beamBox = Num(20m);
        var heightBox = Num(12m);
        var decksBox = Num(3m, min: 1m, max: 20m);
        var deckSpacingBox = Num(4m, min: 2m, max: 10m, format: "0.##");
        var thicknessBox = Num(0.024m, min: 0.005m, max: 0.5m, format: "0.###");
        var spacingBox = Num(1.5m, min: 0.5m, max: 20m, format: "0.##");
        var materialBox = new ComboBox
        {
            ItemsSource = new[] { "steel", "aluminum" },
            SelectedIndex = 0,
            Width = 140,
        };
        var structMatBox = new ComboBox
        {
            ItemsSource = new[] { "steel", "aluminum" },
            SelectedIndex = 0,
            Width = 140,
        };
        var hullBox = new ComboBox
        {
            ItemsSource = new[] { "Faceted", "TaperedBox", "Box", "Cylinder", "Capsule", "LoftedSections" },
            SelectedIndex = 0,
            Width = 140,
        };
        var gravityBox = new ComboBox
        {
            ItemsSource = new[] { "Plating", "None" },
            SelectedIndex = 0,
            Width = 140,
        };
        var gBox = Num(1m, min: 0m, max: 2m, format: "0.##");
        var pressureBox = Num(1m, min: 0.1m, max: 2m, format: "0.##");
        var externalBox = new ComboBox
        {
            ItemsSource = new[] { "Vacuum", "Atmosphere" },
            SelectedIndex = 0,
            Width = 140,
        };

        var apply = new Button
        {
            Content = "Create ship",
            Padding = new Thickness(12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        apply.Click += (_, _) =>
        {
            var def = new ShipDefinition
            {
                Name = string.IsNullOrWhiteSpace(nameBox.Text) ? "New Ship" : nameBox.Text.Trim(),
                Length = ShipLengths.FromMeters((float)(lengthBox.Value ?? 90m)),
                Beam = ShipLengths.FromMeters((float)(beamBox.Value ?? 20m)),
                Height = ShipLengths.FromMeters((float)(heightBox.Value ?? 12m)),
                DeckCount = (int)(decksBox.Value ?? 3m),
                DeckSpacing = ShipLengths.FromMeters((float)(deckSpacingBox.Value ?? 4m)),
                HullMaterial = new MaterialId(materialBox.SelectedItem as string ?? "steel"),
                PrimaryStructuralMaterial = new MaterialId(structMatBox.SelectedItem as string ?? "steel"),
                HullThickness = ShipLengths.FromMeters((float)(thicknessBox.Value ?? 0.024m)),
                FrameSpacing = ShipLengths.FromMeters((float)(spacingBox.Value ?? 1.5m)),
                HullGenerator = (hullBox.SelectedItem as string) switch
                {
                    "Box" => HullGeneratorKind.Box,
                    "TaperedBox" => HullGeneratorKind.TaperedBox,
                    "Cylinder" => HullGeneratorKind.Cylinder,
                    "Capsule" => HullGeneratorKind.Capsule,
                    "LoftedSections" => HullGeneratorKind.LoftedSections,
                    _ => HullGeneratorKind.Faceted,
                },
                GravitySystem = string.Equals(gravityBox.SelectedItem as string, "None", StringComparison.OrdinalIgnoreCase)
                    ? GravitySystemKind.None
                    : GravitySystemKind.Plating,
                NominalGravityG = (float)(gBox.Value ?? 1m),
                NominalInternalPressureAtm = (float)(pressureBox.Value ?? 1m),
                ExternalEnvironment = string.Equals(externalBox.SelectedItem as string, "Atmosphere", StringComparison.OrdinalIgnoreCase)
                    ? ExternalEnvironmentKind.Atmosphere
                    : ExternalEnvironmentKind.Vacuum,
            };
            session.NewShip(def);
            onCreated?.Invoke();
        };

        var form = new StackPanel { Spacing = 8, Margin = new Thickness(8) };
        form.Children.Add(Header("Create ship"));
        form.Children.Add(new TextBlock
        {
            Text = "Clean slate — set definition, then Create ship. PLAN tools place objects on the active deck.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.CadetBlue,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 4),
        });
        form.Children.Add(Labeled("Name", nameBox));
        form.Children.Add(Labeled("Length (m)", lengthBox));
        form.Children.Add(Labeled("Beam (m)", beamBox));
        form.Children.Add(Labeled("Height (m)", heightBox));
        form.Children.Add(Labeled("Deck count", decksBox));
        form.Children.Add(Labeled("Deck spacing (m)", deckSpacingBox));
        form.Children.Add(Labeled("Hull material", materialBox));
        form.Children.Add(Labeled("Structure material", structMatBox));
        form.Children.Add(Labeled("Hull thickness (m)", thicknessBox));
        form.Children.Add(Labeled("Frame spacing (m)", spacingBox));
        form.Children.Add(Labeled("Hull generator", hullBox));
        form.Children.Add(Labeled("Gravity system", gravityBox));
        form.Children.Add(Labeled("Nominal g", gBox));
        form.Children.Add(Labeled("Cabin pressure (atm)", pressureBox));
        form.Children.Add(Labeled("External", externalBox));
        form.Children.Add(apply);
        form.Children.Add(new TextBlock
        {
            Text = "Creates hull, decks, frames, longitudinals, primary bulkheads, environment, and load cases.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            FontSize = 11,
        });
        return form;
    }

    private static NumericUpDown Num(decimal value, decimal min = 1m, decimal max = 500m, string format = "0.##") =>
        new()
        {
            Value = value,
            Minimum = min,
            Maximum = max,
            Increment = format.Contains("###", StringComparison.Ordinal) ? 0.005m : 0.5m,
            FormatString = format,
            Width = 140,
        };

    private static Control Labeled(string label, Control control)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock
        {
            Text = label,
            Width = 140,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.LightGray,
        });
        row.Children.Add(control);
        return row;
    }

    private static TextBlock Header(string text) => new()
    {
        Text = text,
        FontSize = 16,
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(0, 0, 0, 4),
    };
}
