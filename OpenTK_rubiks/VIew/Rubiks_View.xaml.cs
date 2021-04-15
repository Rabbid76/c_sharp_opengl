using System.Windows;
using OpenTK_rubiks.ViewModel;

namespace OpenTK_rubiks.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OpenTK_View : Window
    {
        public OpenTK_View()
        {
            InitializeComponent();
            var vm = this.DataContext as Rubiks_ViewModel;
            vm.Form = this;
        }
    }
}
