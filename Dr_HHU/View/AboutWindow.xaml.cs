using System;
using System.Diagnostics;
using System.Windows.Navigation;
// To access MetroWindow, add the following reference
using MahApps.Metro.Controls;

namespace Dr_HHU.View
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : MetroWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Hyperlink_File(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo("file:\\\\" + Environment.CurrentDirectory + "\\" + e.Uri));
            e.Handled = true;
        }
    }
}
