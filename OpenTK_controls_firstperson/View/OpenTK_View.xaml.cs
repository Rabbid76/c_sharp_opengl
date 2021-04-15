using System.Windows;
using OpenTK_controls_firstperson.ViewModel;

namespace OpenTK_controls_firstperson.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OpenTK_View : Window
    {
        public OpenTK_View()
        {
            InitializeComponent();
            var vm = this.DataContext as OpenTK_ViewModel;
            vm.Form = this;
        }
    }
}
