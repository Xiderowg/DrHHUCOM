using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Dr_HHU.Model;
using System.Net.NetworkInformation;

namespace Dr_HHU.Model
{
    public static class Initializer
    {
        public static bool Init()
        {
            string exePath = Application.ExecutablePath;
            Environment.CurrentDirectory = Path.GetDirectoryName(exePath);

            //初始化Log
            Model.Utils.Log4Net.SetConfig();

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            //防止多启
            Singleton();

            //初始化配置
            Config.Init();

            //初始化必要组件
            Utils.MacAddress.Initialize(GetDevice());
            Model.Utils.Log4Net.WriteLog("初始化程序成功");

            return true;
        }

        private static NetworkInterface GetDevice()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.Speed < 0) continue;
                if (ni.OperationalStatus == OperationalStatus.Down) continue;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    return ni;
                }
            }
            return NetworkInterface.GetAllNetworkInterfaces()[0];
        }

        private static void Singleton()
        {
            /*
            int count = Process.GetProcessesByName(AppDomain.CurrentDomain.FriendlyName.Substring(AppDomain.CurrentDomain.FriendlyName.IndexOf('.'))).Length;
            if (count > 1)
            {
                MessageBox.Show("程序已经运行!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }*/
            bool runone;
            System.Threading.Mutex run = new System.Threading.Mutex(true, "Dr_HHU", out runone);
            if (!runone)
            {
                Process progress1 = GetExistProcess();
                if (progress1 != null)
                {
                    MessageBox.Show("程序已经运行!", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }
        }

        /// <summary>
        /// 查看程序是否已经运行
        /// </summary>
        /// <returns></returns>
        private static Process GetExistProcess()
        {
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process1 in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if ((process1.Id != currentProcess.Id) &&
                     (System.Reflection.Assembly.GetExecutingAssembly().Location == currentProcess.MainModule.FileName))
                {
                    return process1;
                }
            }
            return null;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception ?? new Exception(nameof(e.ExceptionObject));
            Model.Utils.Log4Net.WriteLog(e.ExceptionObject.ToString(), ex);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Model.Utils.Log4Net.WriteLog(e.Exception.Message, e.Exception);
            e.SetObserved();
        }
    }
}
