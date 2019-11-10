using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenTK_WPF_example_1.ViewModel;
using OpenTK; // GLControl

namespace OpenTK_WPF_example_1.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OpenTK_View 
        : Window
    {
        OpenTK_ViewModel _gl_vm = new OpenTK_ViewModel();

        public OpenTK_View()
        {
            InitializeComponent();

            _gl_vm.Formular = this;
            this.DataContext = _gl_vm;
        }

        private void window_view_Loaded(object sender, RoutedEventArgs e)
        {
            // Assign the GLControl as the host control's child.
            host.Child = _gl_vm.GLC; // TODO $$$ [Binding WindowsFormsHost Child property](https://stackoverflow.com/questions/38529025/binding-windowsformshost-child-property) 
        }
    }
}
