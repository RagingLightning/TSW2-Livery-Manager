using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TSW2LM
{
    /// <summary>
    /// Interaction logic for UpdateNotifier.xaml
    /// </summary>
    public partial class UpdateNotifier : Window
    {

        private string link;

        public UpdateNotifier(string installed, string update, string link)
        {
            InitializeComponent();
            lblInstalled.Content = installed;
            lblNew.Content = update;
            this.link = link;
        }

        private void close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void download(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", link);
            this.Close();
        }
    }
}
