using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CxPreflight.UI
{
    /// <summary>Minimal INotifyPropertyChanged base, kept here to avoid a dependency that must load in four Revit versions.</summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Action _run;
        private readonly Func<bool>? _can;

        public RelayCommand(Action run, Func<bool>? can = null)
        {
            _run = run;
            _can = can;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
        public void Execute(object? parameter) => _run();
        public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>A command that runs async work and disables itself while running.</summary>
    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _run;
        private bool _busy;

        public AsyncCommand(Func<Task> run) => _run = run;

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !_busy;

        public async void Execute(object? parameter)
        {
            if (_busy) return;
            _busy = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await _run(); }
            finally
            {
                _busy = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
