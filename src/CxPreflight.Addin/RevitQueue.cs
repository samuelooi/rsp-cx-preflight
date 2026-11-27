using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace CxPreflight.Addin
{
    /// <summary>
    /// The only way the sidebar reaches the Revit API. A modeless pane may not
    /// call the API directly, so each request is queued and run when Revit
    /// raises our ExternalEvent on its API thread. Callers get a Task back.
    /// </summary>
    internal sealed class RevitQueue : IExternalEventHandler
    {
        private readonly ConcurrentQueue<Action<UIApplication>> _work = new ConcurrentQueue<Action<UIApplication>>();
        private readonly ExternalEvent _event;

        /// <summary>Must be created in a valid API context, such as OnStartup.</summary>
        public RevitQueue() => _event = ExternalEvent.Create(this);

        public Task<T> Run<T>(Func<UIApplication, T> work)
        {
            var done = new TaskCompletionSource<T>();
            _work.Enqueue(app =>
            {
                try { done.SetResult(work(app)); }
                catch (Exception ex) { done.SetException(ex); }
            });

            ExternalEventRequest r = _event.Raise();
            if (r == ExternalEventRequest.Denied || r == ExternalEventRequest.TimedOut)
            {
                // Pending is fine: the event is already queued and will drain everything.
                done.TrySetException(new InvalidOperationException("Revit refused the request (" + r + ")."));
            }
            return done.Task;
        }

        public void Execute(UIApplication app)
        {
            while (_work.TryDequeue(out Action<UIApplication>? job)) job(app);
        }

        public string GetName() => "RSP CX Preflight";
    }
}
