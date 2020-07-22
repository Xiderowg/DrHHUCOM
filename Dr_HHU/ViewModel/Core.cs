using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Dr_HHU.Model;
using Dr_HHU.Properties;
using System.Threading;

namespace Dr_HHU.ViewModel
{
    public class Core : BindableProperty, IBaseBinder
    {
        /// <summary>
        ///   抽象
        /// </summary>
        public static Core View
        {
            set;
            get;
        }

        public enum ConnectStatus
        {
            //断开
            Disconnect,
            //连接
            Connect
        }

        public int UserIndex = 0;
        private int _userCount;
        public int UserCount
        {
            set
            {
                _userCount = value;
                Config.usercount = value;
            }
            get => _userCount;
        }
        public string[] UserNames = new string[1];
        public string[] PassWords = new string[1];

        public List<string> UserList
        {
            get
            {
                List<string> result = new List<string>();
                foreach (var name in UserNames)
                {
                    if (!string.IsNullOrEmpty(name))
                        result.Add(name);
                }
                return result;
            }
        }

        public bool IsConnected => ConStatus == ConnectStatus.Connect;

        /// <summary>
        ///     按钮内容
        /// </summary>
        public string BtnContent => IsConnected ? "断开" : "连接";

        public ConnectStatus ConStatus
        {
            set
            {
                _dialStatus = value;
                OnPropertyChanged(nameof(BtnContent));
                OnPropertyChanged();
            }
            get
            {
                return _dialStatus;
            }
        }


        public string[] UserNameToNames(string username)
        {
            var tmp = username.Split(';');
            string[] result = new string[tmp.Length];
            result[0] = "";
            for (int i = 1; i < result.Length; i++)
                result[i] = tmp[i - 1];
            return result;
        }

        public string UserNamesToName(string[] usernames)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(usernames[0]))
            {
                sb.Append(usernames[0]);
                sb.Append(";");
            }
            for (int i = 1; i < usernames.Length; i++)
            {
                if (!string.IsNullOrEmpty(usernames[i]))
                    sb.Append(usernames[i]);
                sb.Append(";");
            }
            return sb.ToString();
        }

        public void ConnectOrDisconnect()
        {
            if (ConStatus == ConnectStatus.Disconnect)
            {
                Connect();
            }
            else if (ConStatus == ConnectStatus.Connect)
            {
                Disconnect();
            }
        }

        /// <summary>
        /// 断开
        /// </summary>
        public void Disconnect(bool isBrutalStop = false)
        {
            if (IsConnected)
            {
                Notify("正在断开连接");
                ConnectBtnEnable = false;
                new Task(() =>
                {
                    Auth.DeAuth(isBrutalStop);
                }).Start();
            }
        }

        public void DisconnectWait(bool isBrutalStop = false)
        {
            if (IsConnected)
            {
                Notify("正在断开连接");
                ConnectBtnEnable = false;
                Auth.DeAuth(isBrutalStop);
            }
        }

        /// <summary>
        /// 重新连接
        /// </summary>
        public void Reconnect()
        {
            if (IsConnected)
            {
                DisconnectWait(isBrutalStop: true); // 只有在检测断网的情况下才会重新连接，所以直接强制注销
            }
            Connect();
        }

        /// <summary>
        /// 连接
        /// </summary>
        public void Connect()
        {
            if (string.IsNullOrEmpty(UserName))
            {
                Notify("请输入账户");
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                Notify("请输入密码");
                return;
            }
            //开始连接
            Notify("正在连接至服务器");

            ConnectBtnEnable = false;

            new Task(() =>
            {
                try
                {
                    // 后台保存
                    Config.SaveConfig();
                    // 初始化认证服务
                    AuthCert ac = new AuthCert(UserName, Password, Model.Utils.MacAddress.MAC.GetAddressBytes());
                    Auth.Initialize(ac);
                    // 开始认证
                    Auth.GoAuth();
                }
                catch (Exception e)
                {
                    Notify(e.Message);
                    Model.Utils.Log4Net.WriteLog(e.Message, e);
                }
                //Enable = true;
            }).Start();
        }

        public NotifyIcon TrayIcon
        {
            set;
            get;
        }

        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        private void InitTrayIcon()
        {
            TrayIcon = new NotifyIcon
            {
                Text = Resources.ProgramTitle,
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                //Icon= SystemIcons.WinLogo,
                Visible = true
            };
        }

        public StatusPresenterModel StatusPresenterModel
        {
            set
            {
                _statusPresenterModel = value;
                OnPropertyChanged();
            }
            get
            {
                return _statusPresenterModel;
            }
        }

        /// <summary>
        ///     通知
        /// </summary>
        /// <param name="str"></param>
        private void Notify(string str)
        {
            StatusPresenterModel.Status = str;
        }

        public void ShowStatus(string status)
        {
            Notify(status);
        }

        /// <summary>
        ///     显示气泡
        /// </summary>
        /// <param name="timeout">消失时间（毫秒）</param>
        /// <param name="title">标题</param>
        /// <param name="text">内容</param>
        /// <param name="icon">图标</param>
        public void ShowBallonTip(int timeout, string title, string text, ToolTipIcon icon)
        {
            TrayIcon.ShowBalloonTip(timeout, title, text, icon);
        }

        /// <summary>
        ///     按钮是否可以按下
        ///     true 可以按下
        ///     false 不可以按下
        /// </summary>
        public bool ConnectBtnEnable
        {
            set
            {
                _connectBtnEnable = value;
                OnPropertyChanged();
            }
            get
            {
                return _connectBtnEnable;
            }
        }

        public string Password
        {
            set
            {
                _password = value;
                //Config.password = value;
                if (UserIndex != -1 && string.IsNullOrEmpty(value))
                {
                    int full = PassWords.Length, flag = 0;
                    string[] tmp = new string[full - 1];
                    for (int i = 0; i < full; i++)
                    {
                        if (i != UserIndex + 1)
                        {
                            tmp[flag] = PassWords[i];
                            flag++;
                        }
                    }
                    PassWords = tmp;
                }
                else
                {
                    PassWords[UserIndex + 1] = value;
                }
                Config.password = UserNamesToName(PassWords);
                OnPropertyChanged();
            }
            get
            {
                return _password;
            }
        }

        public string UserName
        {
            set
            {
                _userName = value;
                //Config.username = value;
                if (UserIndex != -1 && string.IsNullOrEmpty(value))
                {
                    int full = UserNames.Length, flag = 0;
                    string[] tmp = new string[full - 1];
                    for (int i = 0; i < full; i++)
                    {
                        if (i != UserIndex + 1)
                        {
                            tmp[flag] = UserNames[i];
                            flag++;
                        }
                    }
                    UserNames = tmp;
                }
                else
                {
                    UserNames[UserIndex + 1] = value;
                }
                Config.username = UserNamesToName(UserNames);
                OnPropertyChanged();
            }
            get
            {
                return _userName;
            }
        }

        public bool IsRememberPassword
        {
            set
            {
                _isRememberPassword = value;
                OnPropertyChanged();
            }
            get
            {
                return _isRememberPassword;
            }
        }

        public bool IsAutoLogin
        {
            set
            {
                _isAutoLogin = value;
                OnPropertyChanged();
            }
            get
            {
                return _isAutoLogin;
            }
        }

        private bool _isAutoLogin;

        private bool _isRememberPassword;

        private bool _connectBtnEnable = true;

        private ConnectStatus _dialStatus = ConnectStatus.Disconnect;

        private string _password;

        private StatusPresenterModel _statusPresenterModel;

        private string _userName;

        //private int _reconTime = 0;
        //private int _max_reconTime = 5;

        /// <summary>
        /// 从配置获字段
        /// </summary>
        private void InitializeFieldFormDialerConfig()
        {
            if (!string.IsNullOrEmpty(Config.password))
            {
                PassWords = UserNameToNames(Config.password);
                Password = PassWords[UserIndex + 1];
            }

            if (!string.IsNullOrEmpty(Config.username))
            {
                UserNames = UserNameToNames(Config.username);
                UserName = UserNames[UserIndex + 1];
                UserCount = UserNames.Length - 1;
            }

            IsRememberPassword = Config.isRememberPassword;

            IsAutoLogin = Config.isAutoLogin;
        }

        public Core()
        {
            View = this;
            Binder.BaseBinder = this;

            // 初始化
            InitializeFieldFormDialerConfig();
            NewStatusPresenterModel();
            InitTrayIcon();
            ConStatus = ConnectStatus.Disconnect;
        }

        private void NewStatusPresenterModel()
        {
            StatusPresenterModel = new StatusPresenterModel();
            Auth.LoginSuccessEvent += (s, e) =>
            {
                ConStatus = ConnectStatus.Connect;
                ConnectBtnEnable = true;
                StatusPresenterModel.Status = "登录成功！";
                ShowBallonTip(2000, "提示", "登录成功!", ToolTipIcon.Info);
                Model.Utils.Log4Net.WriteLog("登录成功！");
            };
            Auth.LoginFailEvent += (s, e) =>
            {
                ConStatus = ConnectStatus.Disconnect;
                StatusPresenterModel.Status = e.Message;
                ShowBallonTip(2000, "提示", "登录失败!", ToolTipIcon.Error);
                Model.Utils.Log4Net.WriteLog(e.Message);
                ConnectBtnEnable = true;
            };
            Auth.LogoutSuccessEvent += (s, e) =>
            {
                StatusPresenterModel.Status = "注销成功！";
                ShowBallonTip(2000, "提示", "注销成功!", ToolTipIcon.Info);
                Model.Utils.Log4Net.WriteLog("注销成功！");
                ConStatus = ConnectStatus.Disconnect;
                ConnectBtnEnable = true;
            };
            Auth.LogoutFailEvent += (s, e) =>
            {
                StatusPresenterModel.Status = e.Message;
                ShowBallonTip(2000, "提示", "注销失败!", ToolTipIcon.Error);
                Model.Utils.Log4Net.WriteLog(e.Message);
                ConStatus = ConnectStatus.Disconnect;
                ConnectBtnEnable = true;
            };
            Auth.OffsiteLogin += (s, e) =>
            {
                ShowBallonTip(2000, "提示", "检测到异地登陆，您已被迫下线", ToolTipIcon.Info);
                StatusPresenterModel.Status = "检测到异地登陆，您已被迫下线";
                Model.Utils.Log4Net.WriteLog("检测到异地登陆，您已被迫下线");
                Disconnect(isBrutalStop: true);
            };
            Model.Utils.NetworkCheck.InnerNetworkCheckFailed += (s, e) =>
            {
                ShowBallonTip(2000, "提示", "检测到网络异常，请检查与校园网的连接情况！", ToolTipIcon.Error);
                StatusPresenterModel.Status = "检测到网络异常，请检查与校园网的连接情况！";
                Model.Utils.Log4Net.WriteLog("检测到内网异常");
                Disconnect(isBrutalStop: true);
            };
            Model.Utils.NetworkCheck.OuterNetworkCheckFailed += (s, e) =>
            {
                Model.Utils.Log4Net.WriteLog("检测到外网异常");
                if (Config.isNetLostRedail)
                {
                    ShowBallonTip(2000, "提示", "检测到网络波动，正在重新登陆", ToolTipIcon.Error);
                    StatusPresenterModel.Status = "检测到网络异常，请检查与校园网的连接情况！";
                    Reconnect();
                }
                else
                {
                    ShowBallonTip(2000, "提示", "检测到网络波动，请重新登录", ToolTipIcon.Error);
                    StatusPresenterModel.Status = "检测到网络异常，请检查与校园网的连接情况！";
                    Disconnect(isBrutalStop: true);
                }
            };
            Model.Utils.NetworkCheck.OuterNetworkCheckSuccessed += (s, e) =>
            {
                // StatusPresenterModel.Status = "登录成功！";
            };
        }
    }
}
