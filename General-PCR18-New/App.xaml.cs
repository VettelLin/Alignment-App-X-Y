using EnginingDesktop.Util;
using General_PCR18.Common;
using General_PCR18.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace General_PCR18
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private System.Threading.Mutex mutex;

        public App()
        {
            //禁用Backspace快捷键向后回退各个page页
            NavigationCommands.BrowseBack.InputGestures.Clear();
            RegisterEvents();

            this.Startup += new StartupEventHandler(App_Startup);
        }

        private void RegisterEvents()
        {
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;//Task异常 

            //UI线程未捕获异常处理事件（UI主线程）
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                if (e.Exception is Exception exception)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                e.SetObserved();
            }
        }

        //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)      
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                //ignore
            }
        }


        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                HandleException(e.Exception);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                e.Handled = true;
            }

        }

        private static void HandleException(Exception ex)
        {
            LogHelper.Error((object)"程序运行出错", ex);
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // 环境变量
            string libPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "libs");
            AddEnvironmentPaths(new List<string> { libPath });

            //下面的改成自己项目名字或喜欢的一个标识
            mutex = new System.Threading.Mutex(true, "General_PCR18_App", out bool ret);

            if (!ret)
            {
                //MessageBox.Show("已有一个程序实例运行");
                // ActivateOtherWindow();
                //Environment.Exit(0);
                Process process = Process.GetCurrentProcess();
                foreach (Process p in Process.GetProcessesByName("General_PCR18"))
                {
                    // not the same process
                    if (p.Id != process.Id && (p.StartTime - process.StartTime).TotalMilliseconds <= 0)
                    {
                        p.Kill();
                        Thread.Sleep(500);
                    }
                }
            }

            // 先初始化XML配置
            ConfigXMLHelper.ReadXml();

            GlobalData.DeviceCode = SystemInfoUtil.GetUUID();
            GlobalData.SoftVer = CurrentVersion.ToString();

            InitLang();
            InitLog();
        }

        /// <summary>
        /// 添加环境变量
        /// </summary>
        /// <param name="paths">路径列表</param>
        internal static void AddEnvironmentPaths(IEnumerable<string> paths)
        {
            var path = new[] { Environment.GetEnvironmentVariable("PATH") ?? string.Empty };
            string newPath = string.Join(System.IO.Path.PathSeparator.ToString(), path.Concat(paths));
            Environment.SetEnvironmentVariable("PATH", newPath);   // 这种方式只会修改当前进程的环境变量
        }

        private static void ActivateOtherWindow()
        {
            //里面的文本改成自己程序窗口的标题
            var other = Win32API.FindWindow(null, "General-PCR18");
            if (other != IntPtr.Zero)
            {
                Win32API.SetForegroundWindow(other);
                Win32API.ShowWindow(other, 3);
                if (Win32API.IsIconic(other))
                {
                    Win32API.OpenIcon(other);
                }
            }
        }

        private void InitLang()
        {
            try
            {
                ConfigCache configCache = CacheFileUtil.Read();
                if (configCache != null && !string.IsNullOrEmpty(configCache.Lang))
                {
                    Console.WriteLine("Change Lanuage: " + configCache.Lang);
                    Lang.RService.Current.ChangedCulture(configCache.Lang);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error((object)"语言初始化", ex);
            }
        }

        // 初始化日志
        private void InitLog()
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    DirectoryInfo di = new DirectoryInfo(ConfigParam.LogFilePath);
                    if (!di.Exists)
                    {
                        di.Create();
                    }
                    FileInfo[] fi = di.GetFiles("General_PCR18_*.log");
                    DateTime dateTime = DateTime.Now;
                    foreach (FileInfo info in fi)
                    {
                        TimeSpan ts = dateTime.Subtract(info.LastWriteTime);
                        if (ts.TotalDays > ConfigParam.LogFileExistDay)
                        {
                            info.Delete();
                            LogHelper.Debug((object)string.Format("已删除日志。{0}", info.Name));
                        }
                    }
                    LogHelper.Debug((object)"日志清理完毕。");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                LogHelper.Error((object)"LOG处理", ex);
            }
        }


        /// <summary>
        /// 获得当前应用软件的版本
        /// </summary>
        public virtual Version CurrentVersion => new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).ProductVersion);

    }
}
