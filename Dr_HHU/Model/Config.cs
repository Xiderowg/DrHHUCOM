using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_HHU.Model
{
    internal static class Config
    {
        /// <summary>
        /// 用户名数目
        /// </summary>
        public static int usercount = 0;
        /// <summary>
        /// 用户名
        /// </summary>
        public static string username = "";
        /// <summary>
        /// 密码
        /// </summary>
        public static string password = "";

        /// <summary>
        /// 是否记住密码
        /// </summary>
        public static bool isRememberPassword = false;

        /// <summary>
        /// 是否自动登录
        /// </summary>
        public static bool isAutoLogin = false;

        /// <summary>
        /// 是否开机启动
        /// </summary>
        public static bool isRunOnStartup = false;

        /// <summary>
        /// 是否在断网时重连
        /// </summary>
        public static bool isNetLostRedail = true;


        /// <summary>
        /// 是否提示续费提醒
        /// </summary>
        public static bool isRenewalAlert = true;

        /// <summary>
        /// 配置文件版本
        /// </summary>
        public static int configVer = 5;

        /// <summary>
        /// 配置类引用
        /// </summary>
        private static Configuration cfa;

        /// <summary>
        /// 拨号器配置类
        /// </summary>
        public static void Init()
        {
            cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ReadConfig();
        }

        /// <summary>
        /// Bool转换器
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private static string ConvertBoolYN(bool parameter)
        {
            const string True = "Y";
            const string False = "N";

            return parameter ? True : False;
        }

        /// <summary>
        /// 创建配置文件
        /// </summary>
        private static void CreateConfig(int configVer)
        {
            try
            {
                switch (configVer)
                {
                    case 0:
                        cfa.AppSettings.Settings.Add(nameof(username), username);
                        cfa.AppSettings.Settings.Add(nameof(password), isRememberPassword ? password : ";");
                        cfa.AppSettings.Settings.Add(nameof(isAutoLogin), ConvertBoolYN(isAutoLogin));
                        cfa.AppSettings.Settings.Add(nameof(isRememberPassword), ConvertBoolYN(isRememberPassword));
                        cfa.AppSettings.Settings.Add(nameof(isRunOnStartup), ConvertBoolYN(isRunOnStartup));
                        cfa.AppSettings.Settings.Add(nameof(configVer), configVer.ToString());
                        goto case 1;
                    case 1:
                        goto case 2;
                    case 2:
                        cfa.AppSettings.Settings.Add(nameof(isNetLostRedail), ConvertBoolYN(isNetLostRedail));
                        goto case 3;
                    case 3:
                        goto case 4;
                    case 4:
                        cfa.AppSettings.Settings.Add(nameof(isRenewalAlert), ConvertBoolYN(isRenewalAlert));
                        goto case 5;
                    case 5:
                        break;
                    default:
                        goto case 0;
                }

                cfa.Save();
            }
            catch (Exception e)
            {
                Utils.Log4Net.WriteLog(e.Message, e);
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public static void SaveConfig()
        {
            if (cfa.AppSettings.Settings.Count > 0)
            {
                saveConfig();
            }
            else
            {
                CreateConfig(0);
            }
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        private static void saveConfig()
        {
            try
            {
                cfa.AppSettings.Settings[nameof(username)].Value = username;
                string tmp = "";
                for (int i = 0; i < usercount; i++)
                    tmp += ";";
                cfa.AppSettings.Settings[nameof(password)].Value = isRememberPassword ? password : tmp;
                cfa.AppSettings.Settings[nameof(isAutoLogin)].Value = isAutoLogin ? "Y" : "N";
                cfa.AppSettings.Settings[nameof(isRememberPassword)].Value = isRememberPassword ? "Y" : "N";
                cfa.AppSettings.Settings[nameof(isRunOnStartup)].Value = isRunOnStartup ? "Y" : "N";
                cfa.AppSettings.Settings[nameof(isNetLostRedail)].Value = isNetLostRedail ? "Y" : "N";
                cfa.AppSettings.Settings[nameof(configVer)].Value = configVer.ToString();
                cfa.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception e)
            {
                Utils.Log4Net.WriteLog(e.Message, e);
            }
        }

        /// <summary>
        /// 读配置文件
        /// </summary>
        private static void ReadConfig()
        {
            try
            {
                if (cfa.AppSettings.Settings.Count > 0)
                {
                    //升级配置文件
                    CreateConfig(int.Parse(cfa.AppSettings.Settings[nameof(configVer)].Value));

                    username = cfa.AppSettings.Settings[nameof(username)].Value;
                    password = cfa.AppSettings.Settings[nameof(password)].Value;
                    isAutoLogin = cfa.AppSettings.Settings[nameof(isAutoLogin)].Value == "Y";
                    isRememberPassword = cfa.AppSettings.Settings[nameof(isRememberPassword)].Value == "Y";
                    isRunOnStartup = cfa.AppSettings.Settings[nameof(isRunOnStartup)].Value == "Y";
                    isNetLostRedail = cfa.AppSettings.Settings[nameof(isNetLostRedail)].Value == "Y";
                }
                else
                {
                    //创建一个配置文件
                    CreateConfig(0);
                }
            }
            catch (Exception e)
            {
                Utils.Log4Net.WriteLog(e.Message, e);
            }
        }

    }
}
