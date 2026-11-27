using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using CxPreflight.UI;

namespace CxPreflight.Addin
{
    /// <summary>
    /// Start-up. A dockable pane can only be registered while Revit starts,
    /// so the pane, the request queue and the change tracking are all set up here.
    /// </summary>
    public sealed class App : IExternalApplication
    {
        public const string TabName = "RSP CX Preflight";
        public static readonly DockablePaneId PaneId = new DockablePaneId(new Guid("3F6A9C21-8B4E-4D7A-A5C2-91E0B4D6F713"));

        internal static PreflightPanel? Panel { get; private set; }
        private static RevitHost? _host;

        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                var queue = new RevitQueue();
                _host = new RevitHost(queue);
                Panel = new PreflightPanel(_host);

                app.RegisterDockablePane(PaneId, "RSP CX Preflight", new PaneProvider(Panel));
                app.ControlledApplication.DocumentChanged += _host.OnDocumentChanged;

                BuildRibbon(app);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RSP CX Preflight", "Preflight could not start:\n" + ex);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            if (_host != null) app.ControlledApplication.DocumentChanged -= _host.OnDocumentChanged;
            return Result.Succeeded;
        }

        /// <summary>Four buttons, as agreed: the panel, run checks, project setup, project log.</summary>
        private static void BuildRibbon(UIControlledApplication app)
        {
            app.CreateRibbonTab(TabName);
            string dll = Assembly.GetExecutingAssembly().Location;

            RibbonPanel preflight = app.CreateRibbonPanel(TabName, "Preflight");
            Add(preflight, "CxPanel", "Preflight\nPanel", typeof(TogglePanelCommand), "panel",
                "Show or hide the Preflight sidebar.", dll);
            Add(preflight, "CxRun", "Run\nChecks", typeof(RunChecksCommand), "run",
                "Run every check on the active model.", dll);

            RibbonPanel setup = app.CreateRibbonPanel(TabName, "Setup");
            Add(setup, "CxSetup", "Project\nSetup", typeof(ProjectSetupCommand), "setup",
                "Set up the project and the files that make it up (BIM Lead).", dll);
            Add(setup, "CxLog", "Project\nLog", typeof(ProjectLogCommand), "log",
                "Open the project log.", dll);
        }

        private static void Add(RibbonPanel panel, string name, string text, Type command, string icon, string tip, string dll)
        {
            var data = new PushButtonData(name, text, dll, command.FullName)
            {
                ToolTip = tip,
                LargeImage = Icon(icon, 32),
                Image = Icon(icon, 16),
                AvailabilityClassName = typeof(AlwaysAvailable).FullName,
            };
            panel.AddItem(data);
        }

        private static BitmapImage? Icon(string name, int size)
        {
            try
            {
                return new BitmapImage(new Uri(
                    $"pack://application:,,,/CxPreflight.Addin;component/Resources/{name}{size}.png", UriKind.Absolute));
            }
            catch
            {
                return null; // A missing icon must never stop Revit from starting.
            }
        }
    }

    /// <summary>Supplies the sidebar to Revit, docked on the right by default.</summary>
    internal sealed class PaneProvider : IDockablePaneProvider
    {
        private readonly PreflightPanel _panel;

        public PaneProvider(PreflightPanel panel) => _panel = panel;

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = _panel;
            data.VisibleByDefault = true;
            data.InitialState = new DockablePaneState { DockPosition = DockPosition.Right };
        }
    }
}
