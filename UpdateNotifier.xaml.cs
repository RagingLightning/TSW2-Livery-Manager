using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
namespace TSW2LM
{
    /// <summary>
    /// Interaction logic for UpdateNotifier.xaml
    /// </summary>
    public partial class UpdateNotifier : Window
    {

        public UpdateNotifier()
        {
            InitializeComponent();
        }

        private void close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void download(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/RagingLightning/TSW3-LM/releases/latest");
            Close();
        }
    }
}
