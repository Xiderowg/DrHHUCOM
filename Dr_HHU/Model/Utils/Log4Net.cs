﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Config;
using log4net.Appender;
using log4net.Layout;

namespace Dr_HHU.Model.Utils
{
    internal static class Log4Net
    {
        /// <summary>
        /// 设置配置
        /// </summary>
        public static void SetConfig()
        {
            RollingFileAppender fAppender = new RollingFileAppender();
            PatternLayout layout = new PatternLayout
            {
                ConversionPattern = "[%date] %thread -- %-5level -- %logger [%M] -- %message%newline"
            };

            layout.ActivateOptions();

            fAppender.File = "DR_HHU.log";
            fAppender.MaxSizeRollBackups = 10;
            fAppender.MaximumFileSize = "1M";
            fAppender.Layout = layout;
            fAppender.AppendToFile = true;
            fAppender.ActivateOptions();
            BasicConfigurator.Configure(fAppender);
        }

        /// <summary>
        /// 记录普通日志
        /// </summary>
        /// <param name="info">日志内容</param>
        public static void WriteLog(string info)
        {
            if (LogInfo.IsInfoEnabled)
            {
                try
                {
                    LogInfo.Info(info);
                    Console.WriteLine(info);
                }
                catch
                {

                }

            }
        }
        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="info">日志内容</param>
        /// <param name="se">异常</param>
        public static void WriteLog(string info, Exception se)
        {
            if (LogError.IsErrorEnabled)
            {
                try
                {
                    LogError.Error(info, se);
                    Console.WriteLine(info);
                }
                catch
                {

                }

            }
        }

        //log4net日志专用
        private static readonly log4net.ILog LogInfo = log4net.LogManager.GetLogger("loginfo");
        private static readonly log4net.ILog LogError = log4net.LogManager.GetLogger("logerror");
    }
}
