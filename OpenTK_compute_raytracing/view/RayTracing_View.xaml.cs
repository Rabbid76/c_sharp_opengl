using System.Windows;
using OpenTK_compute_raytracing.ViewModel;

namespace OpenTK_compute_raytracing.View

{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class RayTracingView : Window
    {
        public RayTracingView()
        {
            InitializeComponent();
            var vm = this.DataContext as RayTracing_ViewModel;
            vm.Form = this;
        }
    }
}
