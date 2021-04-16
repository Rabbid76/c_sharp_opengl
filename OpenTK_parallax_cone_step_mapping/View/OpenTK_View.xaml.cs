using System.Windows;
using OpenTK_prallax_cone_step_mapping.ViewModel;

namespace OpenTK_prallax_cone_step_mapping.View
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
            var vm = this.DataContext as OpenTK_ViewModel;
            vm.Form = this;
        }
    }
}
