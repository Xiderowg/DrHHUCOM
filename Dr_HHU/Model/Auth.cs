using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dr_HHU.Model
{
    public static class Auth
    {
        public static AuthCert Cert { get; set; }
        static byte[] salt = new byte[4];
        static byte[] md5a = new byte[16];
        static byte[] tail = new byte[16];
        static byte[] tail2 = new byte[4];
        static byte[] flux = new byte[4];
        static int svr_nums;
        static byte[] buffer;
        static IPEndPoint ep;
        static IPAddress ip;
        static Random rand = new Random();

        static System.Timers.Timer timer;

        //static Task LoginTask;
        //static Task LogoutTask;
        //static CancellationTokenSource LoginTaskToken = new CancellationTokenSource();
        //static CancellationTokenSource LogoutTaskToken = new CancellationTokenSource();

        static bool BrutalStop = false;
        static bool Stoped = false;

        // int alivesum = 0;
        static Socket socket;

        public static bool Initialize(AuthCert cert)
        {
            try
            {
                Cert = cert;
                ip = IPAddress.Parse(cert.ServerIP);
                try
                {
                    CleanTask();
                    CloseSocket();
                }
                catch
                { //DO NOTHING
                }
                ep = new IPEndPoint(ip, 61440);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(ep);

                Utils.Log4Net.WriteLog("Socket初始化成功:" + socket.Connected.ToString());

                LoginSuccessEvent += OnLoginSucess;

                return socket.Connected;
            }
            catch (SocketException se)
            {
                Utils.Log4Net.WriteLog("Socket初始化失败", se);
                return false;
            }
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        public static int SendPacket(byte[] buffer, int length)
        {
            try
            {
                return socket.Send(buffer, length, SocketFlags.None);
            }
            catch (ObjectDisposedException ode)
            {
                Utils.Log4Net.WriteLog("Socket被异常关闭", ode);
                return -5;
            }
        }

        /// <summary>
        /// 接受数据包
        /// </summary>
        public static int ReceivePacket()
        {
            try
            {
                int ret = 0;
                buffer = new byte[1024];
                
                while (true)
                {
                    //if (socket.Available < 0) continue;
                    socket.ReceiveTimeout = 3000;
                    try
                    {
                        ret = socket.Receive(buffer, 1024, SocketFlags.None);
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode == 10060)
                        {
                            ret = -1;
                            Utils.Log4Net.WriteLog("无法连接至远程服务器:" + Cert.ServerIP, e);
                        }
                        else
                            throw e;
                    }
                    if (ret < 0)
                    {
                        Utils.Log4Net.WriteLog("无法接受Socket返回信息");
                        return -2;
                    }

                    if (buffer[0] == 0x4D)
                    {
                        if (buffer[1] == 0x15)
                        {
                            OffsiteLogin?.Invoke(null, new Msg("账户在其他地方登陆"));
                            return -5;
                        }

                        continue;
                    }

                    break;
                }
                return ret;
            }
            catch (ObjectDisposedException ode)
            {
                Utils.Log4Net.WriteLog("Socket被异常关闭", ode);
                return -5;
            }
        }

        /// <summary>
        /// 清空 Socket
        /// </summary>
        private static void ClearSocket()
        {
            if (socket == null) return;
            int max_retry_time = 5;
            int i = 0;
            while (true)
            {
                int rec = ReceivePacket();
                if (rec < 0 || i >= max_retry_time)
                    break;
                i += 1;
                Thread.Sleep(300);
            }
            return;
        }

        /// <summary>
        /// 清理Task
        /// </summary>
        private static void CleanTask()
        {
            try
            {
                Stoped = true;
                if (timer != null)
                    timer.Stop();
                /*
                if (LoginTask != null)
                {
                    if (!LoginTask.IsCompleted)
                    {
                        LoginTaskToken.Cancel();
                    }
                    LoginTask.Wait();
                }
                if (LogoutTask != null)
                {
                    if (!LogoutTask.IsCompleted)
                    {
                        LogoutTaskToken.Cancel();
                    }
                    LogoutTask.Wait();
                }
                */
            }
            catch (Exception e)
            {
                Utils.Log4Net.WriteLog(e.Message, e);
            }
            return;
        }

        /// <summary>
        /// 关闭 Socket
        /// </summary>
        private static void CloseSocket()
        {
            if (socket != null)
            {
                socket.Close();
            }
        }


        /// <summary>
        /// 发送Login数据包
        /// </summary>
        /// <returns></returns>
        public static int Login()
        {
            int max_login_time = 3; // TODO Better Implementation is favored
            int i = 0;
            while (true)
            {
                // Make Challenge
                Challenge();
                // Make Login Packet
                Cert.MakeLoginPacket(salt, out buffer, out int buffer_len);
                SendPacket(buffer, buffer_len);
                int recv_len = ReceivePacket();
                if (recv_len <= 0)
                {
                    Utils.Log4Net.WriteLog("接受登录数据包失败，重试中...");
                    i++;
                }
                else
                {
                    if (buffer[0] != 0x04)
                    {
                        if (buffer[0] == 0x05)
                        {
                            LoginFailEvent?.Invoke(null, new Msg("登录失败，错误的用户名或密码！"));
                        }
                        else
                        {
                            LoginFailEvent?.Invoke(null, new Msg("登录失败，无法识别登录返回结果！"));
                        }
                        return -1;
                    }
                }

                if (i >= max_login_time || recv_len > 0)
                    break;
            }
            if (i >= max_login_time)
            {
                LoginFailEvent?.Invoke(null, new Msg("登录失败重试次数过多，取消登录！"));
                return -5;
            }
            // Send Login Successfully
            Utils.Log4Net.WriteLog("发送登录数据包成功！");
            Buffer.BlockCopy(buffer, 0x17, tail, 0, 16);

            return 0;
        }

        /// <summary>
        /// 开始验证
        /// </summary>
        public static void GoAuth()
        {
            CleanTask();
            BrutalStop = false;
            Stoped = false;
            int result = Login();
            ClearSocket();
            if (result == 0)
            {
                KeepAlive_1();
                KeepAlive_2();
                LoginSuccessEvent?.Invoke(null, new Msg("登录成功！"));
            }
            else
            {
                // 发生异常
                CloseSocket();
            }
        }

        /// <summary>
        /// 发送注销请求
        /// </summary>
        /// <returns></returns>
        public static int Logout()
        {
            int max_logout_retry = 2;
            int i = 0;
            //ClearSocket();
            /*
            while (true)
            {
                int result = Challenge();
                if (result != 0)
                {
                    LogoutFailEvent?.Invoke(null, new Msg("Challenge失败，注销失败"));
                    return -1;
                }

                Cert.MakeLogoutPacket(out buffer, out int buffer_len, salt, tail);
                SendPacket(buffer, buffer_len);

                int rec = ReceivePacket();
                if (rec <= 0)
                {
                    Utils.Log4Net.WriteLog("接受注销数据包失败，重试中...");
                    i++;
                }
                else
                {
                    if (buffer[0] != 0x04)
                    {
                        LogoutFailEvent?.Invoke(null, new Msg("无法识别的注销请求返回数据，尝试重新注销"));
                        return -1;
                    }
                    else
                    {
                        break;
                    }
                }
                if (i >= max_logout_retry)
                    break;
            }
            if (i >= max_logout_retry)
            {
                LogoutFailEvent?.Invoke(null, new Msg("注销请求失败次数过多，取消注销"));
                Utils.Log4Net.WriteLog("注销请求失败次数过多，取消注销");
                return -5;
            }
            */
            // 注销数据包发送成功
            return 0;
        }
        /// <summary>
        /// 断开登录过程
        /// </summary>
        public static void DeAuth(bool isBrutalStop)
        {
            CleanTask();
            Utils.NetworkCheck.StopCheck();
            if (isBrutalStop)
            {
                BrutalStop = true;
            }
            else
            {
                int result = Logout();
                if (result != 0)
                {
                    LogoutFailEvent?.Invoke(null, new Msg("注销失败！"));
                }
            }
            ClearSocket();
            LogoutSuccessEvent?.Invoke(null, new Msg("注销成功！"));
        }

        public static void Dispose()
        {
            CleanTask();
            CloseSocket();
            socket = null;
        }

        /// <summary>
        /// 发送Challenge数据包
        /// </summary>
        /// <returns></returns>
        public static int Challenge()
        {
            int max_challenge_time = 5; // TODO Better Implementation is favored
            int i = 0;
            while (true)
            {
                Cert.MakeChallengePacket(out buffer);
                Utils.Log4Net.WriteLog("正在发送Challenge请求");
                SendPacket(buffer, 20);
                int recv_len = ReceivePacket();
                if (recv_len == -5) return -5;
                if (recv_len <= 0)
                {
                    Utils.Log4Net.WriteLog("Challenge超时，重试中...");
                }

                if (buffer[0] != 0x02)
                {
                    Utils.Log4Net.WriteLog("无法识别的Challenge信息，重试中...");
                }
                i++;

                if (recv_len >= 25)
                {
                    Buffer.BlockCopy(buffer, 20, Cert.Host_ip, 0, 4);
                }

                if (i > max_challenge_time || recv_len > 0)
                    break;
            }
            if (i > max_challenge_time)
            {
                //LogoutFailEvent?.Invoke(null, new Msg("Challenge超时，停止重试，Challenge失败。"));
                Utils.Log4Net.WriteLog("Challenge超时，停止重试。");
                return -1;
            }
            Utils.Log4Net.WriteLog("发送Challenge数据包成功");
            Buffer.BlockCopy(buffer, 4, salt, 0, 4);
            return 0;
        }

        /// <summary>
        /// 发送KeepAlive1数据包
        /// </summary>
        /// <returns></returns>
        public static int KeepAlive_1()
        {
            Cert.MakeKeepAlive1Packet(out buffer, out int buffer_len, salt, tail);
            int max_alive1_time = 5;
            int i = 0;
            while (true)
            {
                if (BrutalStop||Stoped) break;
                SendPacket(buffer, buffer_len);
                int recv_len = ReceivePacket();
                if (recv_len < 0)
                {
                    Utils.Log4Net.WriteLog("KeepAlive1请求超时!");
                }

                if (buffer[0] != 0x07)
                {
                    Utils.Log4Net.WriteLog("KeepAlive1请求无法识别!");
                }
                i++;
                if (i >= max_alive1_time || recv_len > 0)
                    break;
            }
            if (i >= max_alive1_time)
            {
                Utils.Log4Net.WriteLog("KeepAlive1请求失败重复次数过多，退出！");
                return -1;
            }
            Utils.Log4Net.WriteLog("KeepAlive1请求成功!");
            return 0;
        }

        /// <summary>
        /// 发送KeepAlive2数据包
        /// </summary>
        /// <returns></returns>
        public static int KeepAlive_2()
        {
            int svr_num = 0;
            int i = 0;
            int max_alive2_time = 5;
            Cert.MakeKeepAlive2Packet(out buffer, out int buffer_len, svr_num, tail2, 1, true);
            while (true)
            {
                if (BrutalStop||Stoped) break;
                SendPacket(buffer, buffer_len);
                int r = ReceivePacket();
                if (r <= 0)
                {
                    Utils.Log4Net.WriteLog("KeepAlive2请求超时，正在重试");
                }
                else
                {
                    if (StartWith(buffer, new byte[4] { 0x07, 0x00, 0x28, 0x00 }) || StartWith(buffer, new byte[4] { 0x07, (byte)svr_num, 0x28, 0x00 }))
                        break;
                    else if (buffer[0] == 0x07 && buffer[2] == 0x10)
                    {
                        svr_num += 1;
                        Utils.Log4Net.WriteLog("KeepAlive2接收到文件包");
                        break;
                    }
                    else
                    {
                        Utils.Log4Net.WriteLog("KeepAlive2请求结果无法识别！正在重试...");
                    }
                }
                i++;
                if (i >= max_alive2_time)
                {
                    Utils.Log4Net.WriteLog("KeepAlive2请求失败重试次数过多，退出！");
                    return -1;
                }
            }
            Utils.Log4Net.WriteLog("KeepAlive2 1号请求发送成功！");
            Cert.MakeKeepAlive2Packet(out buffer, out buffer_len, svr_num, tail2, 1, false);
            SendPacket(buffer, buffer_len);
            i = 0;
            while (true)
            {
                if (BrutalStop||Stoped) break;
                int r = ReceivePacket();
                if (r < 0)
                {
                    Utils.Log4Net.WriteLog("KeepAlive2请求超时！正在重试...");
                }
                else if (buffer[0] == 0x07)
                {
                    svr_num += 1;
                    break;
                }
                else
                {
                    Utils.Log4Net.WriteLog("KeepAlive2请求无法识别！");
                }
                i++;
                if (i >= max_alive2_time)
                {
                    Utils.Log4Net.WriteLog("KeepAlive2请求失败重试次数过多，退出！");
                    return -1;
                }
            }
            // Update tail2
            Buffer.BlockCopy(buffer, 16, tail2, 0, 4);
            Utils.Log4Net.WriteLog("KeepAlive2请求成功！开始KeepAlive！");
            svr_nums = svr_num;
            /*
            i = svr_num;
            while (true)
            {
                Thread.Sleep(20000);
                KeepAlive_1();
                // Keep Alive Packet 4
                Cert.MakeKeepAlive2Packet(out buffer, out buffer_len, i, tail2, 1, false);
                SendPacket(buffer, buffer_len);
                int r = ReceivePacket();
                if (r > 0)
                {
                    // Update tail2
                    Buffer.BlockCopy(buffer, 16, tail2, 0, 4);
                    // KeepAlive Packet 5
                    Cert.MakeKeepAlive2Packet(out buffer, out buffer_len, i + 1, tail2, 3, false);
                    SendPacket(buffer, buffer_len);
                    r= ReceivePacket();
                    if (r > 0)
                    {
                        Buffer.BlockCopy(buffer, 16, tail2, 0, 4);
                        Utils.Log4Net.WriteLog("KeepAlive循环");
                        i = (i + 2) % 127;
                    }
                }
            }
            */
            AliveLoop(null, null);
            timer = new System.Timers.Timer
            {
                Interval = 20000,
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += new System.Timers.ElapsedEventHandler(AliveLoop);
            timer.Start();
            return 0;
        }

        private static void AliveLoop(object sender, EventArgs e)
        {
            if (BrutalStop || Stoped)
            {
                timer.Stop();
                return;
            }
            KeepAlive_1();
            // Keep Alive Packet 4
            Cert.MakeKeepAlive2Packet(out buffer, out int buffer_len, svr_nums, tail2, 1, false);
            SendPacket(buffer, buffer_len);
            int r = ReceivePacket();
            if (r > 0)
            {
                // Update tail2
                Buffer.BlockCopy(buffer, 16, tail2, 0, 4);
                // KeepAlive Packet 5
                Cert.MakeKeepAlive2Packet(out buffer, out buffer_len, svr_nums + 1, tail2, 3, false);
                SendPacket(buffer, buffer_len);
                r = ReceivePacket();
                if (r > 0)
                {
                    Buffer.BlockCopy(buffer, 16, tail2, 0, 4);
                    //Utils.Log4Net.WriteLog("KeepAlive循环");
                    Console.WriteLine("KeepAlive循环");
                    svr_nums = (svr_nums + 2) % 127;
                }
            }
            return;
        }

        private static bool StartWith(byte[] data, byte[] start)
        {
            bool result = true;
            int len = start.Length;
            for (int i = 0; i < len; i++)
            {
                if (data[i] != start[i])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public static void OnLoginSucess(object sender, Msg e)
        {
            //断网检查
            Utils.NetworkCheck.LoopCheck();
        }

        /// <summary>
        /// 登录成功事件
        /// </summary>
        public static EventHandler<Msg> LoginSuccessEvent;
        /// <summary>
        /// 登录失败事件
        /// </summary>
        public static EventHandler<Msg> LoginFailEvent;
        /// <summary>
        /// 注销成功事件
        /// </summary>
        public static EventHandler LogoutSuccessEvent;
        /// <summary>
        /// 注销失败事件
        /// </summary>
        public static EventHandler<Msg> LogoutFailEvent;
        /// <summary>
        /// 异地登陆事件
        /// </summary>
        public static EventHandler<Msg> OffsiteLogin;
    }

    /// <summary>
    /// 简单的消息传输器
    /// </summary>
    public class Msg : EventArgs
    {
        public string Message
        {
            set;
            get;
        }
        public Msg(string _msg)
        {
            Message = _msg;
        }
    }
}
