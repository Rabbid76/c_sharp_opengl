using System.Windows;
using OpenTK_hello_triangle_WPF.ViewModel;

namespace OpenTK_hello_triangle_WPF.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class HelloTriangleView : Window
    {
        public HelloTriangleView()
        {
            InitializeComponent();
            if (DataContext is HelloTriangleViewModel view_model)
                view_model.View = this;
        }
    }
}
