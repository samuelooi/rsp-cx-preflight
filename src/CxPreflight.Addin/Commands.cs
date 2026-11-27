using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CxPreflight.Addin
{
    [Transaction(TransactionMode.Manual)]
    public sealed class TogglePanelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            DockablePane pane = data.Application.GetDockablePane(App.PaneId);
            if (pane.IsShown()) pane.Hide(); else pane.Show();
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public sealed class RunChecksCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            data.Application.GetDockablePane(App.PaneId).Show();
            // Queued: the checks run on the API thread once this command returns.
            App.Panel?.ViewModel.RunChecksCommand.Execute(null);
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public sealed class ProjectSetupCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            TaskDialog.Show("RSP CX Preflight", "Project Setup arrives in P3: the project folder, model files and BIM Lead roles.");
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public sealed class ProjectLogCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            TaskDialog.Show("RSP CX Preflight", "The project log arrives in P3.");
            return Result.Succeeded;
        }
    }

    /// <summary>Keeps the buttons enabled with no model open, so the panel can always be shown.</summary>
    public sealed class AlwaysAvailable : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication app, CategorySet selected) => true;
    }
}
