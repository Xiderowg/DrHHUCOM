using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Dr_HHU.Model
{
    public class AuthCert
    {
        private byte[] _server = { 172, 17, 0, 5 };
        public byte[] Host_ip = { 10, 94, 3, 201 };
        private byte[] _default_dns = { 202, 119, 112, 32 };
        private byte[] _default_dhcp = { 202, 119, 113, 125 };
        private byte[] _secondary_dns = { 202, 119, 112, 32 };
        private byte[] _AUTH_VERSION = { 0x0a, 0x00 };
        private byte[] _KEEPALIVE_VERSION = { 0xdc, 0x02 };
        private byte _CONTROLCHECKSTATUS = 0x20;
        private byte _ADAPTERNUM = 0x05;
        private byte _IPDOG = 0x01;

        private MD5 _md5 = new MD5CryptoServiceProvider();
        public byte[] MD5A;
        private Random rand = new Random();

        /// <summary>
        /// 客户机的以太网卡 Ethernet 的MAC地址，16进制形式。
        /// </summary>
        public byte[] MAC { get; set; }

        /// <summary>
        /// 客户的认证账号
        /// </summary>
        public byte[] Username { get; set; }

        /// <summary>
        /// 客户的认证密码
        /// </summary>
        public byte[] Password { get; set; }

        /// <summary>
        /// 认证服务器IP地址，默认为 172.17.0.5
        /// </summary>
        public string ServerIP
        {
            get
            {
                return string.Format("{0}.{1}.{2}.{3}",
                    (int)_server[0],
                    (int)_server[1],
                    (int)_server[2],
                    (int)_server[3]);
            }
            set
            {
                string[] p = value.Split('.');
                _server[0] = byte.Parse(p[0]);
                _server[1] = byte.Parse(p[1]);
                _server[2] = byte.Parse(p[2]);
                _server[3] = byte.Parse(p[3]);
            }
        }

        /// <summary>
        /// 客户机IP地址，例如 49.140.59.254
        /// </summary>
        public string ClientIP
        {
            get
            {
                return string.Format("{0}.{1}.{2}.{3}",
                    (int)Host_ip[0],
                    (int)Host_ip[1],
                    (int)Host_ip[2],
                    (int)Host_ip[3]);
            }
            set
            {
                string[] p = value.Split('.');
                Host_ip[0] = byte.Parse(p[0]);
                Host_ip[1] = byte.Parse(p[1]);
                Host_ip[2] = byte.Parse(p[2]);
                Host_ip[3] = byte.Parse(p[3]);
            }
        }

        /// <summary>
        /// 客户机的计算机名
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// 客户机的系统
        /// </summary>
        public string HostOS { get; set; }

        /// <summary>
        /// 初始化认证参数
        /// </summary>
        /// <param name=""></param>
        public AuthCert(string username,string password,byte[] mac)
        {
            Username = Encoding.Default.GetBytes(username);
            Password = Encoding.Default.GetBytes(password);
            MAC = mac;
            Host_ip = new byte[] { 10, 97, 174, 165 };
            HostName = "fuyumi";
            HostOS = "Windows 10";
        }
        /// <summary>
        /// 制造握手成功数据包
        /// </summary>
        public byte[] MakeLoginPacket(byte[] salt, out byte[] data, out int len)
        {
            int pwlen = Password.Length;
            byte[] md5_src;
            data = new byte[400];

            int i = 0;

            /*
                struct  _tagLoginPacket
	            {
	                struct _tagDrCOMHeader Header;
	                unsigned char PasswordMd5[MD5_LEN];
	                char Account[ACCOUNT_MAX_LEN];
	                unsigned char ControlCheckStatus;
	                unsigned char AdapterNum;
	                unsigned char MacAddrXORPasswordMD5[MAC_LEN];
	                unsigned char PasswordMd5_2[MD5_LEN];
	                unsigned char HostIpNum;
	                unsigned int HostIPList[HOST_MAX_IP_NUM];
	                unsigned char HalfMD5[8];
	                unsigned char DogFlag;
	                unsigned int unkown2;
	                struct _tagHostInfo HostInfo;
	                unsigned char ClientVerInfoAndInternetMode;
	                unsigned char DogVersion;
	            };
             */

            // Begin
            data[i++] = 0x03;
            data[i++] = 0x01;
            data[i++] = 0x00;
            data[i++] = (byte)(Username.Length + 20);

            // MD5 A
            md5_src = new byte[6 + pwlen];
            md5_src[0] = 0x03;
            md5_src[1] = 0x01;
            Buffer.BlockCopy(salt, 0, md5_src, 2, 4);
            Buffer.BlockCopy(Password, 0, md5_src, 6, pwlen);
            MD5A = _md5.ComputeHash(md5_src);
            Buffer.BlockCopy(MD5A, 0, data, 4, 16);
            i += 16;

            // Username
            Buffer.BlockCopy(Username, 0, data, i, Username.Length);
            i += Username.Length > 36 ? Username.Length : 36;// I dont know whether there is need to truncate here
                                                             // Wait for further test

            // CONTROLCHECKSTATUS ADAPTERNUM
            data[i++] = _CONTROLCHECKSTATUS;
            data[i++] = _ADAPTERNUM;

            /*  dump(int(binascii.hexlify(data[4:10]), 16) ^ mac) */
            Buffer.BlockCopy(MD5A, 0, data, i, 6);
            for (int j = 0; j < 6; j++)
                data[i + j] ^= MAC[j];
            i += 6;

            // MD5 B
            md5_src = new byte[9 + pwlen];
            md5_src[0] = 0x01;
            Buffer.BlockCopy(Password, 0, md5_src, 1, pwlen);
            Buffer.BlockCopy(salt, 0, md5_src, 1 + pwlen, 4);
            Buffer.BlockCopy(_md5.ComputeHash(md5_src), 0, data, i, 16);
            i += 16;

            // Ip number, 1, 2, 3, 4
            data[i++] = 0x01;
            Buffer.BlockCopy(Host_ip, 0, data, i, 4);
            i += 16;

            // MD5 C
            md5_src = new byte[i + 4];
            Buffer.BlockCopy(data, 0, md5_src, 0, i);
            md5_src[i + 0] = 0x14;
            md5_src[i + 1] = 0x00;
            md5_src[i + 2] = 0x07;
            md5_src[i + 3] = 0x0b;
            Buffer.BlockCopy(_md5.ComputeHash(md5_src), 0, data, i, 8);
            i += 8;

            // IPDOG(0x01) 0x00*4
            data[i++] = _IPDOG;
            i += 4;

            /*
                struct  _tagOSVERSIONINFO
	            {
	                unsigned int OSVersionInfoSize;
	                unsigned int MajorVersion;
	                unsigned int MinorVersion;
	                unsigned int BuildNumber;
	                unsigned int PlatformID;
	                char ServicePack[128];
	            };

	            struct  _tagHostInfo
	            {
	                char HostName[HOST_NAME_MAX_LEN];
	                unsigned int DNSIP1;
	                unsigned int DHCPServerIP;
	                unsigned int DNSIP2;
	                unsigned int WINSIP1;
	                unsigned int WINSIP2;
	                struct _tagDrCOM_OSVERSIONINFO OSVersion;
	            };
             */

            // Host Name
            md5_src = Encoding.Default.GetBytes(HostName);
            Buffer.BlockCopy(md5_src, 0, data, i, md5_src.Length > 32 ? 32 : md5_src.Length);
            i += 32;

            // Primary dns
            /*
            data[i++] = 0x0a;
            data[i++] = 0x0a;
            data[i++] = 0x0a;
            data[i++] = 0x0a;
            */
            Buffer.BlockCopy(_default_dns, 0, data, i, 4);
            i += 4;

            // DHCP Server
            Buffer.BlockCopy(_default_dhcp, 0, data, i, 4);
            i += 4;

            // Secondary dns
            /*
            data[i++] = 0xca;
            data[i++] = 0x62;
            data[i++] = 0x12;
            data[i++] = 0x03;
            */
            Buffer.BlockCopy(_secondary_dns, 0, data, i, 4);
            i += 4;



            // WINSIP1 && WINSIP2
            i += 8;

            // OSVersionInfoSize
            data[i++] = 0x94;
            i += 3;
            // MajorVersion
            data[i++] = 0x05;
            i += 3;
            // MinorVersion
            data[i++] = 0x01;
            i += 3;
            // BuildNumber
            data[i++] = 0x28;
            data[i++] = 0x0a;
            i += 2;
            // PlatformID
            data[i++] = 0x02;
            i += 3;

            // ServicePack
            md5_src = Encoding.Default.GetBytes(HostOS);
            Buffer.BlockCopy(md5_src, 0, data, i, md5_src.Length > 32 ? 32 : md5_src.Length);
            i += 128;
            // END OF _tagHostInfo

            // AuthVersion
            Buffer.BlockCopy(_AUTH_VERSION, 0, data, i, 2);
            i += 2;

            // TODO ROR_VERSION NOT IMPLEMENTED

            // _tagDrcomAuthExtData.Code
            data[i++] = 0x02;

            // _tagDrcomAuthExtData.Len
            data[i++] = 0x0c;

            // checksum point
            int check_point = i;
            data[i++] = 0x01;
            data[i++] = 0x26;
            data[i++] = 0x07;
            data[i++] = 0x11;

            // 0x00 0x00 mac
            i += 2;
            Buffer.BlockCopy(MAC, 0, data, i, 6);
            i += 6;

            // CheckSum
            ulong sum = 1234;
            ulong check = 0;
            for (int k = 0; k < i; k += 4)
            {
                check = 0;
                for (int j = 0; j < 4; j++)
                {
                    check = (check << 2) + data[k + j];
                }
                sum ^= check;
            }
            sum = (1968 * sum) & 0xFFFFFFFF;
            for (int j = 0; j < 4; j++)
            {
                data[check_point + j] = (byte)(sum >> (j * 8) & 0x000000FF);
            }

            // auto logout / default: False
            // broadcast mode / default : False
            i += 2;

            // unknown, filled numbers randomly
            data[i++] = 0xe9;
            data[i++] = 0x13;

            len = i;

            Utils.Log4Net.WriteLog("登录握手包已发送");
            return data;
        }

        public void MakeLogoutPacket(out byte[] data,out int len,byte[] salt,byte[] tail)
        {
            int i = 0;
            byte[] md5_src = new byte[6 + Password.Length];
            data = new byte[80];
            data[i++] = 0x06;
            data[i++] = 0x01;
            i++;
            data[i++] = (byte)(Username.Length + 20);

            md5_src[0] = 0x03;
            md5_src[1] = 0x01;
            Buffer.BlockCopy(salt, 0, md5_src, 2, 4);
            Buffer.BlockCopy(Password, 0, md5_src, 6, Password.Length);
            MD5A = _md5.ComputeHash(md5_src);
            Buffer.BlockCopy(MD5A, 0, data, i, 16);
            i += 16;

            Buffer.BlockCopy(Username, 0, data, i, Username.Length > 36 ? 36 : Username.Length);
            i += 36;

            data[i++] = _CONTROLCHECKSTATUS;
            data[i++] = _ADAPTERNUM;

            Buffer.BlockCopy(MD5A, 0, data, i, 6);
            for (int j = 0; j < 6; j++)
                data[i + j] ^= MAC[j];
            i += 6;

            Buffer.BlockCopy(tail, 0, data, i, 16);
            i += 16;

            len = i;
        }

        /// <summary>
        /// 输出一个 Challenge 用的数据包
        /// </summary>
        public void MakeChallengePacket(out byte[] data)
        {
            int i = 0;
            data = new byte[20];
            data[i++] = 0x01;
            data[i++] = 0x02;
            // Random
            data[i++] = (byte)rand.Next(0, 255);
            data[i++] = (byte)rand.Next(0, 255);
            data[i++] = 0x09;
        }

        /// <summary>
        /// 输出一个 Keep-Alive1 用的数据包
        /// </summary>
        public void MakeKeepAlive1Packet(out byte[] data,out int len, byte[] salt, byte[] tail)
        {
            int i = 0;
            len = 42;
            data = new byte[len];
            data[i++] = 0xff;
            // MD52
            Buffer.BlockCopy(MD5A, 0, data, i, 16);
            i += 16;
            // 0x00,0x00,0x00
            i += 3;
            // Tail
            Buffer.BlockCopy(tail, 0, data, i, 16);
            i += 16;
            data[i++] = (byte)rand.Next(0, 255);
            data[i++] = (byte)rand.Next(0, 255);
        }
        /// <summary>
        /// 输出一个 Keep-Alive2 用的数据包
        /// </summary>
        public void MakeKeepAlive2Packet(out byte[] data,out int len,int count,byte[] tail,int type=1,bool isFirst=false)
        {
            int i = 0;
            len = 40;
            data = new byte[len];
            data[i++] = 0x07;
            data[i++] = (byte)count;
            data[i++] = 0x28;
            i++;
            data[i++] = 0x0b;
            data[i++] = (byte)type;
            if (isFirst)
            {
                data[i++] = 0x0f;
                data[i++] = 0x27;
            }
            else
            {
                data[i++] = _KEEPALIVE_VERSION[0];
                data[i++] = _KEEPALIVE_VERSION[1];
            }
            data[i++] = 0x2f;
            data[i++] = 0x12;
            i += 6; 
            Buffer.BlockCopy(tail, 0, data, i, 4);
            i += 8; 
            if (type == 3)
            {
                i += 4; // Fill CRC with zeros
                Buffer.BlockCopy(Host_ip, 0, data, i, 4);
            }
        }
    }
}
