﻿using System.Windows;
using OpenTK_parallax_relief_mapping.ViewModel;

namespace OpenTK_parallax_relief_mapping.View
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

