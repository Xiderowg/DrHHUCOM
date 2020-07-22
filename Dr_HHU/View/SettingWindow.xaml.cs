using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Dr_HHU.Model;

namespace Dr_HHU.View
{
    /// <summary>
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : MetroWindow
    {
        public SettingWindow()
        {
            InitializeComponent();
            StartupCheckBox.IsChecked = Config.isRunOnStartup;
            NetLoopCheckBox.IsChecked = Config.isNetLostRedail;
            RenewalCheckBox.IsChecked = Config.isRenewalAlert;
        }


        private void StartupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (StartupCheckBox.IsChecked != null)
            {
                Config.isRunOnStartup = (bool)StartupCheckBox.IsChecked;
                Model.Utils.RunOnStartup.SetStartup(Config.isRunOnStartup);
            }
        }

        private void NetLoopCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (NetLoopCheckBox.IsChecked != null)
            {
                Config.isNetLostRedail = (bool)NetLoopCheckBox.IsChecked;
            }
        }

        private void RenewalCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (RenewalCheckBox.IsChecked != null)
            {
                Config.isRenewalAlert = (bool)RenewalCheckBox.IsChecked;
            }
        }
    }
}
