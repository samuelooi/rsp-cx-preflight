using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CxPreflight.Core;
using CxPreflight.UI;

namespace CxPreflight.Preview
{
    /// <summary>
    /// The sidebar in a 400 px window, fed by sample data shaped like ABCD BLK3.
    /// "Simulate an edit" raises the same event Revit's DocumentChanged would.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var host = new SampleHost();
            var panel = new PreflightPanel(host);

            var simulate = new Button { Content = "Simulate an edit in Revit", Margin = new Thickness(8), Padding = new Thickness(8, 4, 8, 4) };
            simulate.Click += (_, _) => host.SimulateEdit();

            var root = new DockPanel();
            DockPanel.SetDock(simulate, Dock.Bottom);
            root.Children.Add(simulate);
            root.Children.Add(panel);

            var window = new Window
            {
                Title = "RSP CX Preflight · preview (no Revit)",
                Width = 420,
                Height = 860,
                Content = root,
            };
            new Application().Run(window);
        }
    }

    internal sealed class SampleHost : IPreflightHost
    {
        public event EventHandler<ModelChangedEventArgs>? ModelChanged;

        public async Task<ModelSummary?> RunChecksAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(800, cancellationToken); // feel of a real run
            return new ModelSummary
            {
                DocumentTitle = "ABCD_BLK3_ARC (sample)",
                ElementCount = 26114,
                CheckedAt = DateTime.Now,
                DurationMs = 812,
                Levels = new List<LevelInfo>
                {
                    new LevelInfo("Level 01", 0, false),
                    new LevelInfo("Level 02", 4500, false),
                    new LevelInfo("Level 03", 10450, false),
                    new LevelInfo("Roof", 14200, false),
                    new LevelInfo("T.O. Parapet", 15300, false),
                    new LevelInfo("Ref - FFL", 150, false),
                },
            };
        }

        public void SimulateEdit() => ModelChanged?.Invoke(this, new ModelChangedEventArgs(42, levelsChanged: false));
    }
}
