using System.Windows;
using OpenTK_controls_orbit.ViewModel;

namespace OpenTK_controls_orbit.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OpenTK_View
        : Window
    {
        public OpenTK_View()
        {
            InitializeComponent();
            var vm = this.DataContext as Orbit_ViewModel;
            vm.Form = this;
        }
    }
}