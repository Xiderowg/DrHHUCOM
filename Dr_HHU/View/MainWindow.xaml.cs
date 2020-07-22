using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Dr_HHU.Model;
using Dr_HHU.View;

namespace Dr_HHU
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Core = (ViewModel.Core)DataContext;
            InitTrayIcon();
        }

        private ViewModel.Core Core
        {
            set;
            get;
        }

        /// <summary>
        /// 登陆注销按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Login_Btn_Click(object sender, RoutedEventArgs e)
        {
            Core.ConnectOrDisconnect();
        }

        /// <summary>
        /// 窗体将要关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            Core.ShowBallonTip(3000, "提示：", "退出软件，正在清理Socket资源...", ToolTipIcon.Info);
            //Model.Utils.NetworkCheck.StopCheck();
            if (Core.IsConnected)
                Core.DisconnectWait(isBrutalStop: true);
            Auth.Dispose();
            Config.SaveConfig();
        }

        /// <summary>
        /// 窗体已经关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            Core.TrayIcon.Visible = false;
            Core.TrayIcon.Dispose();
        }

        /// <summary>
        /// 初始化托盘
        /// </summary>
        private void InitTrayIcon()
        {
            Core.TrayIcon.MouseClick += (obj, e) =>
            {
                if ((e.Button == MouseButtons.Left) && (WindowState == WindowState.Minimized))
                {
                    this.Show();
                    this.Activate();
                    WindowState = WindowState.Normal;
                }
            };
        }

        /// <summary>
        /// 记住密码Chk点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Remember_Chk_Click(object sender, RoutedEventArgs e)
        {
            if (AutoLogin_Chk.IsChecked != null &&
                Remember_Chk.IsChecked != null &&
                !(bool)Remember_Chk.IsChecked &&
                (bool)AutoLogin_Chk.IsChecked)
            {
                AutoLogin_Chk.IsChecked = false;
                AutoLogin_Chk_Click(null, null);
            }
            if (Remember_Chk.IsChecked != null)
                Config.isRememberPassword = (bool)Remember_Chk.IsChecked;
        }

        /// <summary>
        /// 记住密码Chk点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoLogin_Chk_Click(object sender, RoutedEventArgs e)
        {
            if (AutoLogin_Chk.IsChecked != null &&
                Remember_Chk.IsChecked != null &&
                !(bool)Remember_Chk.IsChecked &&
                (bool)AutoLogin_Chk.IsChecked)
            {
                Remember_Chk.IsChecked = true;
                Remember_Chk_Click(null, null);
            }
            if (AutoLogin_Chk.IsChecked != null)
                Config.isAutoLogin = (bool)AutoLogin_Chk.IsChecked;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SettingWindow sw = new SettingWindow();
            sw.ShowDialog();
        }

        /// <summary>
        ///     窗口位置改变事件
        ///     处理最小化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            //最小化到托盘
            if (WindowState == WindowState.Minimized)
            {
                Core.ShowBallonTip(3000, "提示", "软件已最小化到托盘", ToolTipIcon.Info);
                Hide();
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Config.isAutoLogin)
                Core.ConnectOrDisconnect();
            if (Config.isRenewalAlert && DateTime.Now.Day == 30)
                Core.ShowBallonTip(2000, "提示", "临近月末，请包月用户注意续费呀！", ToolTipIcon.Info);
        }

        /// <summary>
        /// 用户名列表选择改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Usernames_Txt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (Core != null)
            {
                Core.UserIndex = this.Usernames_Txt.SelectedIndex;
                Core.UserName = this.Usernames_Txt.Text.ToString();
                Core.Password = Core.PassWords[Core.UserIndex + 1];
            }

        }

        /// <summary>
        /// 用户名ComboBox删除按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var closeButton = sender as System.Windows.Controls.Button;
            for (int i = 0; i < Core.UserNames.Length; i++)
            {
                if (Core.UserNames[i] == closeButton.Tag.ToString())
                {
                    Core.UserIndex = i - 1;
                    Core.UserName = "";
                    Core.Password = "";
                    Core.UserIndex = -1;
                    Core.UserCount -= 1;
                    Core.OnPropertyChanged("UserList");
                    break;
                }
            }
        }

        /// <summary>
        /// 用户名框失去焦点事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Usernames_Txt_LostFocus(object sender, RoutedEventArgs e)
        {
            Usernames_Txt_SelectionChanged(null, null);
        }
    }
}
