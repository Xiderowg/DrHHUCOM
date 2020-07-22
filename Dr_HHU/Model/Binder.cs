using System.Windows.Forms;

namespace Dr_HHU.Model
{
    /// <summary>
    /// 
    /// </summary>
    interface IBaseBinder
    {
        void ShowBallonTip(int timeout, string title, string text, ToolTipIcon icon);
        void ShowStatus(string status);
        bool IsConnected { get; }

        void ConnectOrDisconnect();

        string UserName { get; set; }
        string Password { get; set; }
        bool IsRememberPassword { get; set; }
        bool IsAutoLogin { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    internal class Binder
    {
        public static IBaseBinder BaseBinder { get; set; }
    }
}
