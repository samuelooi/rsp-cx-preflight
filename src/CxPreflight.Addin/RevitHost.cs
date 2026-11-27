using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using CxPreflight.Core;

namespace CxPreflight.Addin
{
    /// <summary>
    /// IPreflightHost inside Revit: queues checks onto the API thread and
    /// reports edits made outside the panel.
    /// </summary>
    internal sealed class RevitHost : IPreflightHost
    {
        private readonly RevitQueue _queue;

        public RevitHost(RevitQueue queue) => _queue = queue;

        public event EventHandler<ModelChangedEventArgs>? ModelChanged;

        public Task<ModelSummary?> RunChecksAsync(CancellationToken cancellationToken = default) =>
            _queue.Run<ModelSummary?>(app =>
            {
                Document? doc = app.ActiveUIDocument?.Document;
                if (doc == null || doc.IsFamilyDocument) return null;
                return ModelReader.Read(doc);
            });

        /// <summary>
        /// Called from Revit's DocumentChanged event. Kept cheap on purpose:
        /// counts ids and checks for level edits, no file or network work
        /// (decision 13: logging and tracking must never slow Revit down).
        /// </summary>
        public void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            Document doc = e.GetDocument();
            if (doc.IsFamilyDocument) return;

            int changed = e.GetAddedElementIds().Count + e.GetModifiedElementIds().Count + e.GetDeletedElementIds().Count;
            if (changed == 0) return;

            var levelFilter = new ElementClassFilter(typeof(Level));
            bool levels = e.GetAddedElementIds(levelFilter).Count > 0 || e.GetModifiedElementIds(levelFilter).Count > 0;

            // DocumentChanged runs on Revit's main thread, which is also the pane's UI thread.
            ModelChanged?.Invoke(this, new ModelChangedEventArgs(changed, levels));
        }
    }
}
