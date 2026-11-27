using System.Windows.Controls;
using CxPreflight.Core;

namespace CxPreflight.UI
{
    public partial class PreflightPanel : UserControl
    {
        public PreflightPanel(IPreflightHost host)
        {
            InitializeComponent();
            ViewModel = new PanelViewModel(host);
            DataContext = ViewModel;
        }

        public PanelViewModel ViewModel { get; }
    }
}
