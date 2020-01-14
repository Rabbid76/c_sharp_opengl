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
using OpenTK_parallax_mapping.ViewModel;
using OpenTK; // GLControl

namespace OpenTK_parallax_mapping.View
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

