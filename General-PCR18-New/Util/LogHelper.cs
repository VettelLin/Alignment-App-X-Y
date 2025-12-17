using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using static General_PCR18.Util.ConfigParam;

namespace General_PCR18.Util
{
    /// <summary>
    /// 日志
    /// </summary>
    public class LogHelper
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Debug(object message)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Debug)
            {
                log.Debug(AppendClassLine(message));
                // Always mirror to console for runtime diagnostics
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Debug(string format, params object[] args)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Debug)
            {
                // log.DebugFormat(format, args);
                string message = string.Format(format, args);
                log.Debug(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Info(object message)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Info)
            {
                log.Info(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Info(string format, params object[] args)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Info)
            {
                // log.InfoFormat(format, args);
                string message = string.Format(format, args);
                log.Info(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Warn(object message)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Warn)
            {
                log.Warn(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Warn(string format, params object[] args)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Warn)
            {
                string message = string.Format(format, args);
                log.Warn(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Error(object message)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Error)
            {
                log.Error(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Error(object message, Exception ex)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Error)
            {
                log.Error(message, ex);

                DebugEx(ex);
            }
        }

        public static void Error(string format, params object[] args)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Error)
            {
                // log.ErrorFormat(format, args);
                string message = string.Format(format, args);
                log.Error(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Fatal(object message)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Fatal)
            {
                log.Fatal(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        public static void Fatal(object message, Exception ex)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Fatal)
            {
                log.Fatal(message, ex);

                DebugEx(ex);
            }
        }

        public static void Fatal(string format, params object[] args)
        {
            if ((int)ConfigParam.LogLevel <= (int)LogLevelEnum.Fatal)
            {
                // log.FatalFormat(format, args);
                string message = string.Format(format, args);
                log.Fatal(AppendClassLine(message));
                Console.WriteLine(AppendClassLine(message));
            }
        }

        static string AppendClassLine(object msg)
        {
            string logStr = msg?.ToString();
            //try
            //{
            //    StackTrace st = new StackTrace(true);
            //    StackFrame sf = st.GetFrame(2);
            //    logStr = $"[{Path.GetFileName(sf.GetFileName())}:{sf.GetFileLineNumber()}] {msg}";
            //}
            //catch { }
            return logStr;
        }

        static void DebugEx(Exception ex)
        {
#if DEBUG
            try
            {
                Console.WriteLine("空间名：" + ex.Source + "；" + '\n' +
                                      "方法名：" + ex.TargetSite + '\n' +
                                      "故障点：" + ex.StackTrace.Substring(ex.StackTrace.LastIndexOf("\\") + 1, ex.StackTrace.Length - ex.StackTrace.LastIndexOf("\\") - 1) + '\n' +
                                      "错误提示：" + ex.Message);
            }
            catch
            {
                //
            }
#endif
        }
    }
}
