using System.Windows;
using OpenTK_assimp_example_1.ViewModel;

namespace OpenTK_assimp_example_1.View
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
