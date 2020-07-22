using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dr_HHU.Model.Utils
{
    /// <summary>
    /// 检测是否断网
    /// </summary>
    internal static class NetworkCheck
    {
        /// <summary>
        /// 最大重试次数
        /// </summary>
        public static int MaxRetry = 3;
        /// <summary>
        /// 内网断网事件
        /// </summary>
        public static EventHandler InnerNetworkCheckFailed;
        /// <summary>
        /// 外网断网事件
        /// </summary>
        public static EventHandler OuterNetworkCheckFailed;
        /// <summary>
        /// 外网又连上的事件
        /// </summary>
        public static EventHandler OuterNetworkCheckSuccessed;

        private static CancellationTokenSource cts = null;
        private static CancellationToken ct;

        private static Task pingThread
        {
            set;
            get;
        }

        private static bool _exit = false;

        /// <summary>
        /// 检测
        /// </summary>
        /// <param name="InnerIPAddr">内网IP</param>
        /// <param name="OuterIPAddr">外网IP</param>
        private static void Check(string InnerIPAddr = "172.17.0.5", string OuterIPAddr = "119.75.216.20")
        {
            int innerRetry = 0, outerRetry = 0;
            _exit = false;
            while (!_exit)
            {
                if (ct.IsCancellationRequested) return;
                switch (SimplePing.Ping(InnerIPAddr))
                {
                    case SimplePing.Status.Success:
                        innerRetry = 0;
                        break;
                    case SimplePing.Status.Timeout:
                    case SimplePing.Status.Fail:
                        innerRetry++;
                        break;
                    case SimplePing.Status.Expection:
                        //这就很尴尬了
                        break;
                }

                switch (SimplePing.Ping(OuterIPAddr))
                {
                    case SimplePing.Status.Success:
                        //OuterNetworkCheckSuccessed?.Invoke(null, null);
                        outerRetry = 0;
                        break;
                    case SimplePing.Status.Timeout:
                    case SimplePing.Status.Fail:
                        outerRetry++;
                        break;
                    case SimplePing.Status.Expection:
                        //这就很尴尬了
                        break;
                }

                if (innerRetry > MaxRetry)
                {
                    //事件通知下主线程，没连上网
                    InnerNetworkCheckFailed.Invoke(null, null);
                    return;
                }
                if (outerRetry > MaxRetry)
                {
                    //事件提示下外网断了，重新拨号
                    OuterNetworkCheckFailed?.Invoke(null, null);
                    return;
                }

                // 休息下
                if (innerRetry == 0 || outerRetry == 0)
                    Thread.Sleep(3 * 1000);
            }
        }

        /// <summary>
        /// 循环检测
        /// </summary>
        public static void LoopCheck()
        {
            try
            {
                if (cts != null)
                {
                    cts.Cancel();
                    //cts.Dispose();
                }
                if (pingThread != null)
                {
                    _exit = true;
                    if (pingThread.Status == TaskStatus.Running) pingThread.Wait();
                    pingThread.Dispose();
                }
            }
            catch (Exception e)
            {
                Log4Net.WriteLog(e.Message, e);
            }

            cts = new CancellationTokenSource();
            ct = cts.Token;

            pingThread = new Task(() =>
            {
                Check();
            }, ct);
            pingThread.Start();
        }

        /// <summary>
        /// 停止ping检测
        /// </summary>
        public static void StopCheck()
        {
            if (pingThread == null)
            {
                return;
            }
            if (cts != null)
            {
                try
                {
                    _exit = true;
                    cts.Cancel();
                }
                catch (Exception ex)
                {
                    Log4Net.WriteLog(ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// 简单的PING类
    /// </summary>
    internal static class SimplePing
    {
        /// <summary>
        /// Ping返回状态
        /// </summary>
        public enum Status
        {
            Success = 1,
            Timeout = 0,
            Fail = -1,
            Expection = -2
        }
        /// <summary>
        /// PING
        /// </summary>
        /// <param name="addr">要PING的地址</param>
        /// <returns></returns>
        public static Status Ping(string addr)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(addr);

                if (reply != null)
                {
                    switch (reply.Status)
                    {
                        case IPStatus.Success:
                            return Status.Success;
                        case IPStatus.TimedOut:
                            return Status.Timeout;
                        default:
                            return Status.Fail;
                    }
                }
                return Status.Expection;
            }
            catch (Exception e)
            {
                Log4Net.WriteLog(e.Message, e);
                return Status.Expection;
            }
        }
    }
}
