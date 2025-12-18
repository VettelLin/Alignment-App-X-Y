using General_PCR18.Algorithm;
using General_PCR18.Common;
using General_PCR18.Communication;
using General_PCR18.DB;
using General_PCR18.PageUi;
using General_PCR18.UControl;
using General_PCR18.Util;
using log4net.Repository.Hierarchy;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming.Values;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace General_PCR18
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量区域

        private readonly PageSelect ps; // 选择页
        private DispatcherTimer connectTimer = new DispatcherTimer();
        private Pcr18Client pcr18Client;
        private readonly SynchronizationContext context;

        private readonly SampleRegistrationPage sampleRegistrationPage = new SampleRegistrationPage();
        private readonly RunMonitorPage runMonitorPage = new RunMonitorPage();
        private readonly DataAnalysePage dataAnalysePage = new DataAnalysePage();
        private readonly HeatingDetectionPage heatingDetectionPage = new HeatingDetectionPage();

        private readonly System.Timers.Timer timerReadHeat = new System.Timers.Timer(2000);  // 读取温度线程
        private readonly System.Timers.Timer timerReadLight = new System.Timers.Timer(300);  // 读取光线程
        // 有试管就显示环境温度标签，并开启读取温度线程
        private readonly System.Timers.Timer envTempTimer = new System.Timers.Timer(1000);
        // 1分钟一次读取环境温度
        private readonly System.Timers.Timer envTempUpdateTimer = new System.Timers.Timer(1000);
        // 1分钟一次读取热盖温度
        private readonly System.Timers.Timer hotCoverTempUpdateTimer = new System.Timers.Timer(1000);

        private bool IsDebug = false; // DEBUG 模式
        private bool IsLoopEEP = false; // 是否循环下发 EPPROM 命令
        private bool IsSendEEP = true; // 下发 EPPROM 命令

        private bool readComplete = false;
        private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        private bool msgShow = false;
        private static readonly object msgShowLock = new object();
        // 记录试管插入，10分钟后自动扫描光
        private readonly System.Timers.Timer[] tubeCountdownTimers = new System.Timers.Timer[18];
        // 加热总时间，10分钟 = 600s
        private readonly int heatingTime = 10 * 1;
        // Lysis 阶段（裂解）：秒
        private readonly int LysisTime = 7 * 1;
        // Valving 阶段（阀控）：秒
        private readonly int ValvingTime = 3 * 1;
        // 当前倒计时, ms
        private readonly int[] tubeCountdownCurrent = new int[18];

        #endregion

        // ===== Trace helpers for DPI/Window diagnostics =====
        private static void WriteTrace(string msg)
        {
            try
            {
                Debug.WriteLine(msg);
                var log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dpi_trace.log");
                File.AppendAllText(log, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}");
            }
            catch { }
        }

        private void TracePoint(string tag)
        {
            try
            {
                var wa = SystemParameters.WorkArea;
                var dpi = VisualTreeHelper.GetDpi(this);
                WriteTrace($"[TRACE {tag}] State={WindowState} Top={Top:0} Left={Left:0} " +
                           $"W/H={Width:0}x{Height:0} Actual={ActualWidth:0}x{ActualHeight:0} " +
                           $"WorkArea={wa.Width:0}x{wa.Height:0} DPI={dpi.DpiScaleX:0.##}x{dpi.DpiScaleY:0.##}");
            }
            catch { }
        }

        private void TraceScreens(string tag)
        {
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                WriteTrace($"[SCREENS {tag}] Count={screens.Length} PrimaryBounds={System.Windows.Forms.Screen.PrimaryScreen.Bounds}");
                foreach (var s in screens)
                {
                    var b = s.Bounds; var wa = s.WorkingArea;
                    WriteTrace($"[SCREEN] Primary={s.Primary} Bounds={b.X},{b.Y},{b.Width}x{b.Height} WorkArea={wa.X},{wa.Y},{wa.Width}x{wa.Height}");
                }
                WriteTrace($"[SCREENS {tag}] WPF Primary={SystemParameters.PrimaryScreenWidth:0}x{SystemParameters.PrimaryScreenHeight:0} Virtual={SystemParameters.VirtualScreenWidth:0}x{SystemParameters.VirtualScreenHeight:0}");
            }
            catch { }
        }

        private void ApplyMaximizedBounds()
        {
            try
            {
                var wa = SystemParameters.WorkArea; // WPF 逻辑坐标，已考虑 DPI
                // 采用无边框 + 直接设置为工作区大小，避免覆盖任务栏
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Normal;
                var dpi = VisualTreeHelper.GetDpi(this);
                // 安全边距：为避免边缘裁切，按 DPI 给出 4dip 的余量
                double guardX = Math.Ceiling(4 * dpi.DpiScaleX);
                double guardY = Math.Ceiling(4 * dpi.DpiScaleY);
                Left = wa.Left;
                Top = wa.Top;
                Width = Math.Max(1, wa.Width - guardX);
                Height = Math.Max(1, wa.Height - guardY);
                TracePoint("ApplyMaximizedBounds");
                ApplyContentScale();
            }
            catch { }
        }

        private void ApplyContentScale()
        {
            try
            {
                // 设计期基准尺寸：以 1536x960（你机器上最大化的可视高度）为准
                const double designW = 1536.0;
                const double designH = 960.0;
                var wa = SystemParameters.WorkArea;
                double sx = Math.Min(wa.Width / designW, 1.0);
                double sy = Math.Min(wa.Height / designH, 1.0);
                double s = Math.Min(sx, sy); // 等比缩放，优先确保纵向能完全显示
                if (FrameWork != null)
                {
                    FrameWork.LayoutTransform = new ScaleTransform(s, s);
                    TracePoint($"ApplyContentScale s={s:0.###}");
                }
            }
            catch { }
        }

        public MainWindow()
        {
            InitializeComponent();

            ps = new PageSelect(); //实例化PageSelect，初始选择页ps
            ps.LoadedEventTick += Ps_LoadedEventTick;
            FrameWork.Content = new Frame() { Content = ps };

            connectTimer.Interval = TimeSpan.FromSeconds(3);
            //connectTimer.Tick += ConnectTimer_Tick;
            connectTimer.Start();

            // 串口
            pcr18Client = new Pcr18Client();
            pcr18Client.DataReceived += Pcr18Client_DataReceived;

            context = SynchronizationContext.Current; // 获取当前 UI 线程的上下文

            // 订阅事件
            EventBus.OnMainMessageReceived += EventBus_OnMessageReceived;
            // 快捷键：F8 打开 X 轴对齐选择并启动
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            // 读取温度线程
            timerReadHeat.AutoReset = true;
            timerReadHeat.Enabled = false;
            timerReadHeat.Elapsed += TimerReadHeatTime_Elapsed;

            // 读取光线程
            timerReadLight.AutoReset = true;
            timerReadLight.Enabled = false;
            timerReadLight.Elapsed += TimerReadLight_Elapsed;

            // 环境温度显示线程
            envTempTimer.AutoReset = true;
            envTempTimer.Enabled = false;
            envTempTimer.Elapsed += EnvTempTimer_Elapsed;
            // 环境温度更新线程
            envTempUpdateTimer.AutoReset = true;
            envTempUpdateTimer.Enabled = false;
            envTempUpdateTimer.Elapsed += EnvTempUpdateTimer_Elapsed;
            // 热盖温度
            hotCoverTempUpdateTimer.AutoReset = true;
            hotCoverTempUpdateTimer.Enabled = false;
            hotCoverTempUpdateTimer.Elapsed += HotCoverTempUpdateTimer_Elapsed;

            // 10分钟后扫描光
            for (int i = 0; i < tubeCountdownTimers.Length; i++)
            {
                tubeCountdownTimers[i] = new System.Timers.Timer(1000);
                tubeCountdownTimers[i].Elapsed += TubeCountdownTimer_Elapsed;
                tubeCountdownTimers[i].AutoReset = true;
            }

            this.Loaded += Windows_Loaded;
            this.Closing += Windows_Closing;
            this.Closed += Windows_Closed;
            this.StateChanged += MainWindow_StateChanged;
            this.Activated += MainWindow_Activated;

            // Trace bindings
            TracePoint("Ctor");
            TraceScreens("Ctor");
            this.ContentRendered += (_, __) => TracePoint("ContentRendered");
            this.SizeChanged += (_, __) => TracePoint("SizeChanged");
        }

        private void HotCoverTempUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            System.Timers.Timer t = sender as System.Timers.Timer;
            if (t.Interval == 1000)
            {
                // 1 分钟
                t.Interval = 1000 * 60;
            }
            LogHelper.Info("获取热盖温度");
            pcr18Client.HotCoverTemp();
        }

        /// <summary>
        /// Read env temp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnvTempUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            System.Timers.Timer t = sender as System.Timers.Timer;
            if (t.Interval == 1000)
            {
                // 1 分钟
                t.Interval = 1000 * 60;
            }

            Thread.Sleep(300);
            LogHelper.Info("获取环境温度");
            pcr18Client.EnvTemp();
        }

        /// <summary>
        /// 环境温度tag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnvTempTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //int index = Array.IndexOf(GlobalData.DS.PCRKeyStatus, true);
            //if (index == -1)
            //{
            //    envTempUpdateTimer.Stop();
            //    EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.HideEnvTempTag });
            //    return;
            //}

            //EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.ShowEnvTempTag });

            //// 1分分钟更新一次温度
            //if (!envTempUpdateTimer.Enabled)
            //{
            //    envTempUpdateTimer.Enabled = true;
            //}
        }

        /// <summary>
        /// 开始倒计时
        /// </summary>
        /// <param name="tubeIndex"></param>
        public void StartCountdown(int tubeIndex, bool sendCmd = false)
        {
            // 试管插入，倒计时结束自动扫描光
            LogHelper.Debug("试管 {0} 开始加热倒计时 {1} ms. 是否下发加热: {2}", tubeIndex, heatingTime, sendCmd);

            GlobalData.SetStatus(tubeIndex, TUBE_STATUS.Heating);  // 加热中

            SendRefreshUIEvent();  // 刷新UI事件

            if (sendCmd)
            {
                pcr18Client.StartHeat((byte)tubeIndex);
            }
            tubeCountdownTimers[tubeIndex].Start();
            tubeCountdownCurrent[tubeIndex] = 0;

            // Lysis 阶段（裂解）：
            EventBus.RunMonitor(new NotificationMessage()
            {
                Code = MessageCode.StageCountdown,
                TubeIndex = tubeIndex,
                CountdownSeconds = LysisTime,
                CountdownTitle = "Lysis: 7 mins"
            });
        }

        /// <summary>
        /// 倒计时处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TubeCountdownTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            int tubeIndex = Array.IndexOf(tubeCountdownTimers, timer);

            // Console.WriteLine("tubeIndex: " + tubeCountdownCurrent[tubeIndex]);

            if (tubeCountdownCurrent[tubeIndex] == LysisTime)
            {
                // Valving 阶段（阀控）
                EventBus.RunMonitor(new NotificationMessage()
                {
                    Code = MessageCode.StageCountdown,
                    TubeIndex = tubeIndex,
                    CountdownSeconds = ValvingTime,
                    CountdownTitle = "Valving: 3 mins"
                });
            }
            else if (tubeCountdownCurrent[tubeIndex] == heatingTime)
            {
                tubeCountdownCurrent[tubeIndex] = 0;
                timer.Stop();

                LogHelper.Debug("MainWindows-试管 {0} 加热倒计时 {1} ms完成，开始扫描", tubeIndex, heatingTime);

                // 开始扫描
                EventBus.MainMsg(new MainNotificationMessage() { Code = MainMessageCode.LightStart, TubeIndex = tubeIndex });
                return;
            }

            tubeCountdownCurrent[tubeIndex]++;
        }

        private void Windows_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("===>MainWindow Loaded");
            loading.Visibility = Visibility.Visible;

            TracePoint("Loaded");
            TraceScreens("Loaded");


            // 移除强制多屏定位，避免在 DPI>100% 时出现位置/尺寸错乱
            ApplyMaximizedBounds();

            CheckInstrumentTask();

            username.Text = GlobalData.CurrentUser?.Name;

            // 开启获取环境，热盖温度线程
            envTempUpdateTimer.Start();
            hotCoverTempUpdateTimer.Start();
        }

        // 最大化按钮
        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Maximized;
                if (sender is RoutedEventArgs)
                {
                }
            }
            catch { }
            try { (e as RoutedEventArgs).Handled = true; } catch { }
        }

        // 在保持窗口无边框/最大化的前提下，允许按住顶部栏拖拽到其它屏幕
        private bool _maybeDragFromTitle = false;
        private Point _titleMouseDownPoint;

        private const double TitleButtonsGuardWidth = 240; // 右上角按钮保护宽度
        private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!this.IsEnabled) return; // 有模态对话框时不处理拖拽
                // 若点在交互控件上（按钮、输入框、下拉等），不触发拖拽，交给控件自身处理
                if (IsInteractiveElement(e.OriginalSource as DependencyObject))
                {
                    WriteTrace("[TRACE TitleBarDown] in interactive element -> pass-through");
                    return;
                }
                try
                {
                    if (TitleBar != null)
                    {
                        Point pt = e.GetPosition(TitleBar);
                        WriteTrace($"[TRACE TitleBarDown] ptX={pt.X:0.##} titleW={TitleBar.ActualWidth:0.##} guardW={TitleButtonsGuardWidth}");
                        if (pt.X >= Math.Max(0, TitleBar.ActualWidth - TitleButtonsGuardWidth))
                        {
                            WriteTrace("[TRACE TitleBarDown] in right-button guard area -> pass-through");
                            return; // 在右上角按钮区域，禁止开启拖拽
                        }
                    }
                }
                catch { }
                if (this.WindowStyle != WindowStyle.None) return;
                if (e.ButtonState != MouseButtonState.Pressed) return;

                // 暂不还原窗口，仅记录可能的拖拽起点，等移动达到阈值再开始拖拽
                _titleMouseDownPoint = e.GetPosition(this);
                _maybeDragFromTitle = true;
                WriteTrace("[TRACE TitleBarDown] maybe-drag=true");
            }
            catch { }
        }

        private static bool IsInteractiveElement(DependencyObject obj)
        {
            while (obj != null)
            {
                if (obj is Button || obj is TextBox || obj is ComboBox || obj is MenuItem || obj is Slider || obj is System.Windows.Controls.Primitives.ToggleButton || obj is ListBoxItem)
                {
                    return true;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
            return false;
        }

        private bool _isDraggingFromTitle = false;
        private bool _isDraggingFromEdge = false;
        private bool _suspendAutoMaximize = false; // 拖拽过程中暂时禁止自动最大化
        private const int EdgeDragMargin = 40; // px within window edge to start drag
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled) return;
            if (IsInteractiveElement(e.OriginalSource as DependencyObject)) return;

            if (_maybeDragFromTitle && e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(this);
                if (Math.Abs(p.X - _titleMouseDownPoint.X) > 3 || Math.Abs(p.Y - _titleMouseDownPoint.Y) > 3)
                {
                    WriteTrace("[TRACE DragStart] begin title drag");
                    // 真正开始拖拽：先从最大化还原再 DragMove
                    if (this.WindowState == WindowState.Maximized)
                    {
                        double rx = p.X / this.ActualWidth;
                        double ry = p.Y / this.ActualHeight;
                        Rect restore = this.RestoreBounds;
                        _suspendAutoMaximize = true;
                        this.WindowState = WindowState.Normal;
                        this.Left = System.Windows.Forms.Control.MousePosition.X - restore.Width * rx;
                        this.Top = System.Windows.Forms.Control.MousePosition.Y - restore.Height * ry;
                    }
                    _isDraggingFromTitle = true;
                    _maybeDragFromTitle = false;
                    try { this.DragMove(); } catch { }
                    // 结束后自动回到最大化
                    if (this.WindowState != WindowState.Maximized) this.WindowState = WindowState.Maximized;
                    _suspendAutoMaximize = false;
                    _isDraggingFromTitle = false;
                    WriteTrace("[TRACE DragEnd] finish title drag");
                }
            }
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsInteractiveElement(e.OriginalSource as DependencyObject)) return;
            _isDraggingFromTitle = false;
            _maybeDragFromTitle = false;
            if (this.WindowState != WindowState.Maximized) this.WindowState = WindowState.Maximized;
            _suspendAutoMaximize = false;
        }

        private void WindowEdge_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!this.IsEnabled) return; // 有模态对话框时不处理拖拽
                if (this.WindowStyle != WindowStyle.None) return;
                if (e.ButtonState != MouseButtonState.Pressed) return;

                // 如果点击在交互控件（包含右上角按钮）上，直接放行，避免拦截点击
                if (IsInteractiveElement(e.OriginalSource as DependencyObject))
                {
                    WriteTrace("[TRACE EdgeDown] interactive element -> pass-through");
                    return;
                }

                Point p = e.GetPosition(this);
                // 顶部标题栏区域交给标题栏事件处理，避免与按钮冲突
                try
                {
                    if (TitleBar != null)
                    {
                        double th = Math.Max(0, TitleBar.ActualHeight);
                        if (p.Y <= th + 2)
                        {
                            // 顶部边缘：允许拖拽，但避开右侧按钮保护区
                            Point tpt = Mouse.GetPosition(TitleBar);
                            double rightGuardStart = Math.Max(0, TitleBar.ActualWidth - TitleButtonsGuardWidth);
                            if (tpt.X >= rightGuardStart)
                            {
                                WriteTrace("[TRACE EdgeDown] top-edge over buttons -> pass-through");
                                return;
                            }
                            WriteTrace("[TRACE EdgeDown] top-edge drag allowed");
                        }
                    }
                }
                catch { }

                bool nearEdge = p.X <= EdgeDragMargin || p.X >= this.ActualWidth - EdgeDragMargin
                                || p.Y <= EdgeDragMargin || p.Y >= this.ActualHeight - EdgeDragMargin;
                if (!nearEdge) return;
                WriteTrace($"[TRACE EdgeDown] nearEdge=true at {p.X:0},{p.Y:0}");

                // 临时还原并准备拖拽
                if (this.WindowState == WindowState.Maximized)
                {
                    double rx = p.X / this.ActualWidth;
                    double ry = p.Y / this.ActualHeight;
                    Rect restore = this.RestoreBounds;
                    _suspendAutoMaximize = true;
                    this.WindowState = WindowState.Normal;
                    this.Left = System.Windows.Forms.Control.MousePosition.X - restore.Width * rx;
                    this.Top = System.Windows.Forms.Control.MousePosition.Y - restore.Height * ry;
                }
                _isDraggingFromEdge = true;
            }
            catch { }
        }

        private void WindowEdge_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingFromEdge && e.LeftButton == MouseButtonState.Pressed)
            {
                try { this.DragMove(); } catch { }
            }
        }

        private void WindowEdge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingFromEdge = false;
            if (this.WindowState != WindowState.Maximized) this.WindowState = WindowState.Maximized;
            _suspendAutoMaximize = false;
        }

        private bool _wasMinimized = false;
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // 仅记录是否处于最小化；不立即强制切换，避免系统还原过程中比例抖动
            _wasMinimized = this.WindowState == WindowState.Minimized;
            TracePoint($"StateChanged->{this.WindowState}");
            // 当从最大化到 Normal（例如拖拽结束/系统还原）时，立即重新应用工作区边界与缩放，避免留下偏移
            if (this.WindowState == WindowState.Normal && !_suspendAutoMaximize)
            {
                ApplyMaximizedBounds();
            }
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            // 从任务栏激活后，若刚经历最小化，则异步切换到最大化，避免布局在同一帧内两次变更
            if (_wasMinimized)
            {
                _wasMinimized = false;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try { if (this.WindowState != WindowState.Maximized) this.WindowState = WindowState.Maximized; } catch { }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            TracePoint("Activated");
        }

        // 取消强制最大化的重写，避免在弹窗/焦点切换时引发位置抖动
        //protected override void OnStateChanged(EventArgs e)
        //{
        //    base.OnStateChanged(e);
        //}

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            // 获取当前的焦点控件
            var element = FocusManager.GetFocusedElement(this);

            // 如果当前有焦点控件，且点击的区域不在该控件上，则清除焦点
            if (element != null && !element.IsMouseOver)
            {
                // 将焦点设置到窗口本身或其他透明控件
                FocusManager.SetFocusedElement(this, this);
            }

            // 继续处理默认的鼠标按下事件
            base.OnPreviewMouseDown(e);
        }

        private void Ps_LoadedEventTick(PageSelect sender, bool loaded)
        {
            // 默认页            
            NavPage(sampleRegistrationPage);
        }

        private void Windows_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Windows_Closed(object sender, EventArgs e)
        {
            CloseWin();
        }

        private void CloseWin()
        {
            System.Windows.Application.Current.Shutdown();
            System.Environment.Exit(0);
        }

        private void SendRefreshUIEvent()
        {
            EventBus.DataAnalyse(new NotificationMessage { Code = MessageCode.RefreshUI });
            EventBus.HeatingDection(new NotificationMessage { Code = MessageCode.RefreshUI });
            EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.RefreshUI });
            EventBus.SampleRegistration(new NotificationMessage { Code = MessageCode.RefreshUI });
        }

        private readonly object _readLightLock = new object();

        private void StartReadLightTimer()
        {
            lock (_readLightLock)
            {
                LogHelper.Debug("开始启动扫描Timer。之前状态：{0}", timerReadLight.Enabled);
                if (!timerReadLight.Enabled)
                {
                    timerReadLight.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 处理全局事件
        /// </summary>
        /// <param name="obj"></param>
        private void EventBus_OnMessageReceived(MainNotificationMessage obj)
        {
            switch (obj.Code)
            {
                case MainMessageCode.SavePatienInfo:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        int tubeIndex = obj.TubeIndex;
                        Task.Run(async () =>
                        {
                            await Task.Delay(1);
                            SavePatientInfo(tubeIndex);
                        });

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
                case MainMessageCode.HeatStart:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        try
                        {
                            if (!GlobalData.DS.PCRStatus || !pcr18Client.IsOpen)
                            {
                                ShowDeivceNotConnectError();
                                return;
                            }

                            // 处理加热流程
                            int tubeIndex = obj.TubeIndex;
                            int h1 = GlobalData.DS.HeatH1Temp[tubeIndex];
                            int h3 = GlobalData.DS.HeatH3Temp[tubeIndex];
                            int t1 = GlobalData.DS.HeatH1Time[tubeIndex];
                            int t3 = GlobalData.DS.HeatH3Time[tubeIndex];

                            pcr18Client.SetHeatTemp((byte)tubeIndex, h1, h3);
                            Thread.Sleep(100);
                            pcr18Client.SetHeatTime((byte)tubeIndex, t1, t3);
                            Thread.Sleep(100);
                            pcr18Client.StartHeat((byte)tubeIndex);

                            // 更改状态
                            GlobalData.SetStatus(tubeIndex, TUBE_STATUS.Heating);

                            // 刷新UI事件
                            SendRefreshUIEvent();

                            LogHelper.Debug((object)("MainWindows-开始加热, 试管序号: " + tubeIndex));

                            // 启动获取温度线程
                            ReadHeatTimeCmd();
                            if (!timerReadHeat.Enabled)
                            {
                                timerReadHeat.Enabled = true;
                            }

                            // 回调
                            obj.Callback?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error(ex);

                            context.Post(_ =>
                            {
                                MyMessageBox.CustomMessageBoxResult result =
                                MyMessageBox.Show(Properties.Resources.msg_op_error,
                                MyMessageBox.CustomMessageBoxButton.OK,
                                MyMessageBox.CustomMessageBoxIcon.Warning);
                            }, null);
                        }
                    }
                    break;
                case MainMessageCode.HeatStop:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        // 停止加热
                        int tubeIndex = obj.TubeIndex;
                        string key = tubeIndex.ToString();
                        lock (key)
                        {
                            pcr18Client.StopHeat((byte)tubeIndex);
                            tubeCountdownTimers[tubeIndex].Stop();

                            // 更改状态
                            GlobalData.SetStatus(tubeIndex, TUBE_STATUS.HeatingCompleted);

                            // 刷新UI事件
                            SendRefreshUIEvent();

                            // 回调
                            obj.Callback?.Invoke();
                        }
                    }
                    break;
                case MainMessageCode.HeatingCountdown:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        // 开始加热倒计时
                        int tubeIndex = obj.TubeIndex;
                        string key = tubeIndex.ToString();
                        lock (key)
                        {
                            StartCountdown(tubeIndex, true);

                            // 回调
                            obj.Callback?.Invoke();
                        }
                    }
                    break;
                case MainMessageCode.LightStart:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        // 开始获取光数据
                        int tubeIndex = obj.TubeIndex;
                        //int waitSeconds = GlobalData.DS.HeatH1Time[tubeIndex] + 7;
                        string key = tubeIndex.ToString();
                        lock (key)
                        {
                            GlobalData.SetStatus(tubeIndex, TUBE_STATUS.Lighting);
                            // 本轮开始前请求先行 Y 轴前移 1mm
                            preYShiftRequest = true;
                            preYShiftDone = false;
                            EventBus.RunMonitor(new NotificationMessage() { Code = MessageCode.StartDetectionTime, TubeIndex = tubeIndex });

                            // 刷新UI事件
                            SendRefreshUIEvent();

                            // 开始扫描timer
                            StartReadLightTimer();

                            // 回调
                            obj.Callback?.Invoke();
                        }
                    }
                    break;
                case MainMessageCode.LightPause:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        // 暂停光扫描，清空之前的数据
                        int tubeIndex = obj.TubeIndex;
                        string key = tubeIndex.ToString();
                        lock (key)
                        {
                            GlobalData.SetStatus(tubeIndex, TUBE_STATUS.LightingPaused);

                            // 刷新UI事件
                            SendRefreshUIEvent();

                            // 回调
                            obj.Callback?.Invoke();
                        }
                    }
                    break;
                case MainMessageCode.LightStop:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        // 停止光扫描
                        int tubeIndex = obj.TubeIndex;
                        string key = tubeIndex.ToString();
                        lock (key)
                        {
                            GlobalData.SetStatus(tubeIndex, TUBE_STATUS.LightingCompleted);

                            // 刷新UI事件
                            SendRefreshUIEvent();

                            // 回调
                            obj.Callback?.Invoke();
                        }
                    }
                    break;
                case MainMessageCode.AutoExportLight:
                    {
                        LogHelper.Debug("MainWindows收到消息: {0}, {1} ", obj.Code, obj.TubeIndex);

                        // 自动导出数据
                        int tubeIndex = obj.TubeIndex;
                        string key = tubeIndex.ToString();
                        lock (key)
                        {
                            // 组装包含登记信息的文件名：孔位-时间-样本ID-患者ID-类型
                            string dock = Tools.GetDockUnit(tubeIndex);
                            string ts = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
                            string sampleId = Tools.SanitizeFileName(GlobalData.DS.HeatSampleID[tubeIndex] ?? "");
                            string patientId = Tools.SanitizeFileName(GlobalData.DS.HeatPatientID[tubeIndex] ?? "");
                            int typeId = GlobalData.DS.HeatSampleType[tubeIndex];
                            string typeText = VarDef.SampleType.ContainsKey(typeId)
                                ? Tools.SanitizeFileName(VarDef.SampleType[typeId][0])
                                : "";

                            string extra = string.Join("-", new[] { sampleId, patientId, typeText }.Where(s => !string.IsNullOrWhiteSpace(s)));
                            string filename = string.IsNullOrWhiteSpace(extra)
                                ? ($"{dock}-{ts}.xlsx")
                                : ($"{dock}-{ts}-{extra}.xlsx");

                            List<int> tubeList = new List<int>() { tubeIndex };

                            // 1) 系统备份路径（来自设置页 ConfigCache.DataPath；若为空则回退 C:\）
                            try
                            {
                                var config = CacheFileUtil.Read();
                                string systemDir = (config != null && !string.IsNullOrWhiteSpace(config.DataPath))
                                    ? config.DataPath
                                    : @"C:\\";

                                try
                                {
                                    if (!Directory.Exists(systemDir))
                                    {
                                        Directory.CreateDirectory(systemDir);
                                    }
                                }
                                catch { }

                                string systemPath = Path.Combine(systemDir, filename);
                                ExportLightData(tubeList, systemPath);
                                LogHelper.Debug($"AutoExport 保存到系统路径: {systemPath}");
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error(ex);
                            }

                            // 2) 用户本次实验自定义路径（来自 Sample 界面 GlobalData.BackupDataPath）
                            try
                            {
                                string userDir = GlobalData.BackupDataPath;
                                if (!string.IsNullOrWhiteSpace(userDir))
                                {
                                    // 若与系统路径相同则不重复导出
                                    bool sameAsSystem = false;
                                    try
                                    {
                                        var config = CacheFileUtil.Read();
                                        string systemDir = (config != null && !string.IsNullOrWhiteSpace(config.DataPath))
                                            ? config.DataPath
                                            : @"C:\\";
                                        sameAsSystem = string.Equals(
                                            Path.GetFullPath(userDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                            Path.GetFullPath(systemDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                            StringComparison.OrdinalIgnoreCase);
                                    }
                                    catch { }

                                    if (!sameAsSystem)
                                    {
                                        try
                                        {
                                            if (!Directory.Exists(userDir))
                                            {
                                                Directory.CreateDirectory(userDir);
                                            }
                                        }
                                        catch { }

                                        string userPath = Path.Combine(userDir, filename);
                                        ExportLightData(tubeList, userPath);
                                        LogHelper.Debug($"AutoExport 保存到用户路径: {userPath}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error(ex);
                            }

                            // 回调
                            obj.Callback?.Invoke();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 定时读取温度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerReadHeatTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
#if DEBUG
            LogHelper.Debug((object)"Main 线程中读取温度");
#endif

            ReadHeatTimeCmd();
        }

        private void ReadHeatTimeCmd()
        {
            List<int> tubeIndexs = new List<int>();
            for (int i = 0; i < 18; i++)
            {
                if (GlobalData.GetStatus(i) == TUBE_STATUS.Heating)
                {
                    tubeIndexs.Add(i);
                }
            }

#if DEBUG
            LogHelper.Debug("Main 线程 ReadHeatTimeCmd 试管数量：{0}", tubeIndexs.Count);
#endif

            if (tubeIndexs.Count > 0)
            {
                pcr18Client.ReadHeatTemp(tubeIndexs);
            }
        }

        private readonly object lockLight = new object();
        private int lightCycle = 0;

        // 首步 Y 轴预移动（1 mm）控制
        private bool preYShiftRequest = false;   // 需要执行一次 Y 轴前移 1mm
        private bool preYShiftDone = false;      // 本轮已完成

        // X 轴 Alignment 测试控制
        private bool xAlignMode = false;                 // 是否处于 X 对齐测试
        private int xAlignIdx = 0;                       // 当前候选索引（0..6）
        private int xAlignCyclesDone = 0;                // 当前候选已完成的轮次
		private const int xAlignCyclesPerCandidate = 2;  // 每个候选跑 2 轮
		private readonly double[] xAlignBest = new double[8]; // 每个候选的最大 raw（现有 8 组）
        private int xAlignChannel = 0;                   // 选定通道：0=FAM,1=Cy5,2=VIC(HEX),3=Cy5.5,4=ROX
        private string xAlignChannelName = "FAM";        // 记录通道名称用于提示
        private bool xAlignPresetSentThisCandidate = false; // 本候选是否已下发 5 条写入命令
		private bool xAlignResultShown = false;          // 结果弹窗只显示一次
		private int xAlignLastScoredLightCycle = -1;     // 上次已计分的 lightCycle，防重复计分
		private int xAlignScoredCount = 0;              // 累计已计分的轮次数（全体候选总和）
		private readonly int[] xAlignLastDataCount = new int[5]; // 各通道上次数据点数，防止未采样就计分
		private bool xAlignFinalizePending = false;      // 达到计分阈值后，等待空档弹窗
		private bool autoStartYAfterX = false;          // X 完成后不再自动开始 Y 对齐（改为手动按 F9）
		private bool pendingStartYAfterX = false;       // 延迟到下一轮 case0 空档再启动 Y 对齐
		private DateTime xAlignPresetCooldownUntil = DateTime.MinValue; // 锁定最佳组后给设备的冷却时间
		private DateTime xAlignGroupCooldownUntil = DateTime.MinValue;  // 每组候选预设写入后的冷却时间
		private readonly string[] xAlignLabels = new string[]
        {
            "-2.00 mm → 541, 1016, 1500, 1982, 2458",
            "-1.50 mm → 572, 1047, 1531, 2013, 2489",
            "-0.50 mm → 635, 1110, 1594, 2076, 2552",
            "-0.375 mm → 643, 1118, 1602, 2084, 2560",
            "-0.1875 mm → 655, 1130, 1614, 2096, 2572",
            "-0.3125 mm → 647, 1122, 1606, 2088, 2564",
			"-0.25 mm → 651, 1126, 1610, 2092, 2568",
			"0.00 mm → 667, 1142, 1626, 2108, 2584",
        };

		// Y 轴 Alignment 测试控制（通过调整每轮开始前的 Y 轴预移动量）
		private bool yAlignMode = false;                 // 是否处于 Y 对齐测试
		private int yAlignIdx = 0;                       // 当前候选索引（0..3）
		private int yAlignCyclesDone = 0;                // 当前候选已完成的轮次
		private const int yAlignCyclesPerCandidate = 2;  // 每个候选跑 2 轮
		private readonly double[] yAlignBest = new double[4]; // 每个候选的最大 raw
		private int yAlignChannel = 0;                   // 选定通道
		private string yAlignChannelName = "FAM";        // 记录通道名称
		private bool yAlignResultShown = false;          // 结果弹窗仅一次
		private int yAlignLastScoredLightCycle = -1;     // 防重复计分
		private int yAlignScoredCount = 0;               // 累计计分
		private readonly double[] yAlignCandidatesMm = new double[] { 1.50, 1.25, 1.125, 1.375 };
		private readonly string[] yAlignLabels = new string[] { "1.50 mm", "1.25 mm", "1.125 mm", "1.375 mm" };

		// 当前生效的 Y 轴预移动与行间补偿（非对齐模式时使用）
		private double currentYPreMoveMm = 1.375;        // 默认预移动 1.375 mm
		private double currentYRowExtraMm = 1.0;         // 行间额外前移（B/C），默认 1.0 mm

		private double GetCurrentPreMoveMm()
		{
			if (yAlignMode) return yAlignCandidatesMm[Math.Max(0, Math.Min(yAlignCandidatesMm.Length - 1, yAlignIdx))];
			return currentYPreMoveMm;
		}

		private double GetYExtraMmForRows()
		{
			if (yAlignMode) return yAlignCandidatesMm[Math.Max(0, Math.Min(yAlignCandidatesMm.Length - 1, yAlignIdx))];
			return currentYRowExtraMm;
		}

        // 扫描步骤 ACK 等待控制（只等待“已接收/开始执行”的回执，不等待动作完成）
        private volatile bool motorAckPending = false;      // 当前步骤是否正在等待 ACK
        private volatile bool motorAckReceived = false;     // 是否已收到 ACK（由串口接收线程置位）
        private int motorAckStep = -1;                      // 等待 ACK 的步骤索引
        private DateTime motorAckLastSend = DateTime.MinValue; // 上次发送时间
        private int motorAckRetries = 0;                    // 已重试次数
        private const int MotorAckTimeoutMs = 10000;         // ACK 等待超时（仅接收确认，不等动作完成）
        private const int MotorAckMaxRetries = 4;           // 超时重试次数上限
        // 插管事件后的 ACK 超时宽限控制
        private DateTime lastTubeEventAt = DateTime.MinValue; // 最近一次 5EE800 到达时间
        private const int TubeEventWindowMs = 300;             // 插管事件窗口（ms）
        private const int TubeEventAckGraceMs = 1000;          // 窗口内额外放宽的超时（ms）
        // 软继续策略：当 ACK 重试耗尽时，保守等待后继续推进，避免整轮中断
        private const bool SoftContinueOnAckTimeout = true;    // 启用软继续（风险：可能与设备不同步）
        private int softContinueConsecutive = 0;               // 连续软继续计数
        private const int SoftContinueConsecutiveLimit = int.MaxValue;    // 上限（放宽为无限次软继续）
        private static int GetSoftContinueDelayMs(int step)
        {
            // 针对位移步骤给更长保护延时；扫描/归位给较短保护
            switch (step)
            {
                case 1: return 2000; // Y 轴移动 6 行
                case 3: return 2000; // Y 轴移动 11 行（缩短软继续等待，避免长时间停滞）
                case 5: return 800;  // X 轴归位
                default: return 500; // 其它步骤（X 正/反向扫描）
            }
        }

        // 针对 serpentine 步骤的 ACK 期望：默认 0x87；step0 允许 0x92（0x12 写入类回执）以避免等待卡死
        private bool IsExpectedAckForStep(string ackHead, int step)
        {
            if (string.Equals(ackHead, "5E8700", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 对 step0（X 正向扫描前）容忍 5E9200，以避免 0x12 写入回执占先导致长时间等待
            if (step == 0 && string.Equals(ackHead, "5E9200", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 定时读取光
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerReadLight_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (lockLight)
            {
                ReadLigthCmd();
            }
        }

        private void ReadLigthCmd()
        {
			LogHelper.Debug($"===>Main 扫描中... physCycle={lightCycle + 1} (zero-based={lightCycle})");

            GlobalData.LastLightType = 0;

			// 汇总一次 X/Y 对齐与电机状态，便于你复制排查
			try
			{
				double groupCd = Math.Max(0, (xAlignGroupCooldownUntil - DateTime.Now).TotalMilliseconds);
				double presetCd = Math.Max(0, (xAlignPresetCooldownUntil - DateTime.Now).TotalMilliseconds);
				LogHelper.Debug(
					$"[Tick] cycle={lightCycle} step={GlobalData.DS.RunMonitorMotorYIndex} " +
					$"xMode={xAlignMode} xIdx={xAlignIdx} xDone={xAlignCyclesDone}/{xAlignCyclesPerCandidate} xScored={xAlignScoredCount} xSent5={xAlignPresetSentThisCandidate} " +
					$"yMode={yAlignMode} yIdx={yAlignIdx} yDone={yAlignCyclesDone}/{yAlignCyclesPerCandidate} yScored={yAlignScoredCount} " +
					$"groupCooldown={groupCd:0}ms presetCooldown={presetCd:0}ms " +
					$"motorPending={motorAckPending} motorStep={motorAckStep} retries={motorAckRetries}");
			}
			catch { }

            // 若开启 X 对齐模式且准备开始本候选，则在进入 case0 之前下发 5 条写入命令
            if (xAlignMode && !xAlignPresetSentThisCandidate && !motorAckPending
                && (GlobalData.DS.RunMonitorMotorYIndex == 0))
            {
                try
                {
                    LogHelper.Debug($"[X-Align] 下发候选 {xAlignIdx}: {xAlignLabels[Math.Max(0, Math.Min(xAlignLabels.Length-1, xAlignIdx))]}");
                    pcr18Client.SetXAlignmentPreset(xAlignIdx);
                    xAlignPresetSentThisCandidate = true;
                    // 每组预设写入后增加冷却期，避免紧接着扫描
                    xAlignGroupCooldownUntil = DateTime.Now.AddMilliseconds(1000);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                }
            }

            // 若本轮需要先执行一次 Y 轴前移 1mm，则优先处理该预移动步骤（不改变 case 流程编号）
			if (preYShiftRequest && !preYShiftDone && !motorAckPending)
            {
				// 动态预移动：mm -> 格数（1 格 = 9 mm）
				double grids = GetCurrentPreMoveMm() / 9.0;
				LogHelper.Debug((object)$"预步骤：Y 轴前移 {GetCurrentPreMoveMm():0.###} mm ({GetCurrentPreMoveMm():0.###}/9 格)");
                SendOrRetryMotorCmd(() =>
                {
					pcr18Client.MotorControlYAbsolute4Byte(grids);
                    Thread.Sleep(100);
                }, 100); // 使用 100 作为预移动的 step 编号
                return;
            }

            int i = 0;
            for (; i < 18; i++)
            {
                if (GlobalData.GetStatus(i) == TUBE_STATUS.Lighting)
                {
                    break;
                }
            }

            // 判断电机是否走完整轮
            bool cycleCompleted = GlobalData.DS.RunMonitorMotorYIndex == 0 || GlobalData.DS.RunMonitorMotorYIndex == 13;

            // 没有样本
            if (i >= 18 && cycleCompleted)
            {
                // **所有管子都停止，执行归位命令**
                LogHelper.Debug("所有测试已停止，执行归位。共扫描了 {0} 轮", lightCycle);

                GlobalData.DS.RunMonitorMotorYIndex = 0;
                timerReadLight.Enabled = false;
                lightCycle = 0;

                pcr18Client.MoveMotorYHome();
                Thread.Sleep(3000);
                pcr18Client.MoveMotorXHome();
                Thread.Sleep(100);
                // X轴归位，未确定使用
                //pcr18Client.MotorControl5();

                return;
            }

            // 有样本
            if (!pcr18Client.IsOpen)
            {
                LogHelper.Debug("端口 {0} 未连接", pcr18Client.PortName);
                return;
            }

            // 读取EPPROM
            if (IsSendEEP && GlobalData.DS.RunMonitorMotorYIndex < 12)
            {
                List<string> eepList = new List<string>() {
                        "5E 11 00 09 00 21 F2 0E 99",
                        "5E 11 00 09 00 20 27 01 C0",
                        "5E 11 00 09 00 05 50 02 CF",
                        "5E 12 00 09 00 20 27 5A 1A",
                        "5E 43 00 05 A6",
                        "5E 11 00 09 00 13 00 02 8D",
                        "5E 11 00 09 00 13 20 02 AD",
                        "5E 11 00 09 00 13 40 02 CD",
                        "5E 11 00 09 00 13 60 02 ED",
                        "5E 11 00 09 00 13 80 02 0D",
                    };

                // 顺序下发EEPROM命令，等待收到回应再发下一条
                foreach (string eep in eepList)
                {
                    int times = 0;
                    readComplete = false;
                    pcr18Client.SendData(eep, "ReadEEPROM");
                    //while (!readComplete && times < 5000) { Thread.Sleep(1); times++; }
                }

                pcr18Client.MotorPreScanCommands();
            }

            if (!IsLoopEEP)
            {
                IsSendEEP = false;
            }

            // 若当前在等待 ACK，则检查是否收到或是否需要重试
            if (motorAckPending)
            {
                if (motorAckReceived)
                {
                    // 针对位移类步骤（Y轴绝对移动）增加保护延时，避免动作叠加
                    int protectDelayMs = 0;
                    if (motorAckStep == 1) // Y 轴移动 6 行
                    {
                        protectDelayMs = 2000; // 提高保护延时，确保物理到位
                    }
                    else if (motorAckStep == 3) // Y 轴移动 11 行
                    {
                        protectDelayMs = 3500; // 更长位移，保护更久
                    }
                    else if (motorAckStep == 100) // 预移动 1mm
                    {
                        protectDelayMs = 500;
                    }
                    if (protectDelayMs > 0)
                    {
                        LogHelper.Debug((object)$"Protect delay for step {motorAckStep}: {protectDelayMs} ms");
                        Thread.Sleep(protectDelayMs);
                    }

                    // 若是预移动 1mm 的 ACK，清理等待状态但不推进 case 编号（保持从 0 开始）
                    if (motorAckStep == 100)
                    {
                        motorAckPending = false;
                        motorAckReceived = false;
                        motorAckRetries = 0;
                        motorAckStep = -1;
                        preYShiftRequest = false;
                        preYShiftDone = true;
                        LogHelper.Debug((object)"预步骤 ACK 完成，保持步骤编号不变，继续执行 case 0");
                        return;
                    }
                    else
                    {
                        motorAckPending = false;
                        motorAckReceived = false;
                        motorAckRetries = 0;
                        motorAckStep = -1;
                        GlobalData.DS.RunMonitorMotorYIndex++; // 收到接收确认后推进到下一步
                        LogHelper.Debug((object)"Received command ACK，Go to Next Setp");
                    }
                    return; // 本周期已处理
                }

                // 未收到，则看是否超时需要重发（插管事件后的短窗口内放宽超时）
                double elapsedMs = (DateTime.Now - motorAckLastSend).TotalMilliseconds;
                int timeoutMs = MotorAckTimeoutMs;
                double sinceTubeMs = (DateTime.Now - lastTubeEventAt).TotalMilliseconds;
                if (sinceTubeMs >= 0 && sinceTubeMs <= TubeEventWindowMs)
                {
                    timeoutMs += TubeEventAckGraceMs;
                    LogHelper.Debug((object)$"TubeEvent grace: extend ACK timeout to {timeoutMs} ms (elapsed={elapsedMs:0} ms)");
                }
                if (elapsedMs > timeoutMs)
                {
                    if (motorAckRetries < MotorAckMaxRetries)
                    {
                        LogHelper.Debug((object)$"Motor ACK Overtime，REPEAT {motorAckStep} Command（the {motorAckRetries + 1} times）");
                        // 重新发送当前步骤命令
                        ResendMotorCmd(motorAckStep);
                        motorAckRetries++;
                        motorAckLastSend = DateTime.Now;
                    }
                    else
                    {
                        // 重试耗尽
                        if (SoftContinueOnAckTimeout && softContinueConsecutive < SoftContinueConsecutiveLimit)
                        {
                            int guard = GetSoftContinueDelayMs(motorAckStep);
                            LogHelper.Error((object)$"Motor ACK timeout exhausted at step {motorAckStep}. Soft-continue after {guard} ms (#{softContinueConsecutive + 1}).");
                            try { Thread.Sleep(guard); } catch { }

                            // 清理等待状态并保守推进到下一步
                            motorAckPending = false;
                            motorAckReceived = false;
                            motorAckRetries = 0;
                            softContinueConsecutive++;
                            GlobalData.DS.RunMonitorMotorYIndex++; // 软推进
                            return; // 本周期已处理，下一周期进入下一步
                        }
                        else
                        {
                            // 达到上限（已放宽为无限）仍采用软继续，去除弹窗不中断实验
                            int guard2 = GetSoftContinueDelayMs(motorAckStep);
                            LogHelper.Error((object)$"Motor ACK timeout exhausted at step {motorAckStep}. Force soft-continue after {guard2} ms.");
                            try { Thread.Sleep(guard2); } catch { }

                            motorAckPending = false;
                            motorAckReceived = false;
                            motorAckRetries = 0;
                            softContinueConsecutive++;
                            GlobalData.DS.RunMonitorMotorYIndex++; // 软推进
                            return;
                        }
                    }
                }

                // 仍在等待，下次周期再检查
                LogHelper.Debug((object)$"Waiting motor  ACK ，step {motorAckStep}");
                return;
            }

            switch (GlobalData.DS.RunMonitorMotorYIndex)
            {
                case 0:
                    // 只要进入新的步骤（不在等待中），重置软继续计数
                    softContinueConsecutive = 0;
					// 若 X 对齐刚刚锁定了最佳组，则等待冷却时间结束再发送扫描指令
					if (!motorAckPending && DateTime.Now < xAlignPresetCooldownUntil)
					{
						LogHelper.Debug($"[X-Align] 冷却中，推迟 case0 扫描 {(xAlignPresetCooldownUntil - DateTime.Now).TotalMilliseconds:0} ms");
						return;
					}
					// 若 X 对齐计分已达阈值且结果待弹窗，且当前无等待 ACK，优先在 case0 空档完成弹窗与收尾
					if (xAlignFinalizePending && !motorAckPending && xAlignMode)
					{
						try
						{
							int bestIdx = 0;
							double bestVal = double.MinValue;
							for (int k = 0; k < xAlignBest.Length; k++)
							{
								if (xAlignBest[k] > bestVal)
								{
									bestVal = xAlignBest[k];
									bestIdx = k;
								}
							}

							try
							{
								pcr18Client.SetXAlignmentPreset(bestIdx);
								xAlignPresetCooldownUntil = DateTime.Now.AddMilliseconds(800);
							}
							catch (Exception ex)
							{
								LogHelper.Error(ex);
							}

							var sb = new StringBuilder();
							sb.AppendLine($"X 轴最佳组：{xAlignLabels[bestIdx]}");
							sb.AppendLine($"通道：{xAlignChannelName}");
							sb.AppendLine($"最佳峰值：{bestVal:0.##}");
							sb.AppendLine();
							sb.AppendLine("各组峰值：");
							for (int iList = 0; iList < xAlignLabels.Length; iList++)
							{
								double v = xAlignBest[iList];
								string vs = (v == double.MinValue) ? "-" : v.ToString("0.##");
								sb.AppendLine($"- {xAlignLabels[iList]}  →  {vs}");
							}
							string msg = sb.ToString();
							LogHelper.Info("[X-Align] 完成。 " + msg.Replace("\n", " | "));
							Dispatcher.Invoke(() =>
							{
								System.Windows.MessageBox.Show(this, msg, "Alignment 结果", MessageBoxButton.OK, MessageBoxImage.Information);
							});

							xAlignMode = false;
							xAlignResultShown = true;
							xAlignFinalizePending = false;
							try
							{
								timerReadLight.Enabled = false;
								GlobalData.DS.RunMonitorMotorYIndex = 0;
								LogHelper.Info("[X-Align] 已完成并停止扫描。请按 F9 选择通道，开始 Y 轴校准。");
							}
							catch (Exception exStop)
							{
								LogHelper.Error(exStop);
							}
						}
						catch (Exception ex)
						{
							LogHelper.Error(ex);
						}
						return;
					}
					// 若已完成 X 对齐且计划自动启动 Y 对齐，则在 case0 空档并确保无 ACK 等待时延迟约 300ms 再启动
					if (pendingStartYAfterX && !xAlignMode && !yAlignMode && !motorAckPending)
					{
						pendingStartYAfterX = false;
						try
						{
							LogHelper.Info("[Y-Align] 准备在 case0 空档启动（延迟 300ms）");
							Task.Run(async () =>
							{
								await Task.Delay(300);
								try
								{
									Dispatcher.Invoke(() => StartYAlignmentTest(xAlignChannelName));
								}
								catch (Exception ex)
								{
									LogHelper.Error(ex);
								}
							});
						}
						catch (Exception ex)
						{
							LogHelper.Error(ex);
						}
					}
                    // X 对齐：预设写入已在进入 case0 之前完成，这里不重复下发，避免双发导致设备忙碌与 ACK 竞争
					// 每轮开始前优先执行一次 Y 轴前移（候选或当前值）
                    if (preYShiftRequest && !preYShiftDone && !motorAckPending)
                    {
						double grids = GetCurrentPreMoveMm() / 9.0;
						LogHelper.Debug((object)$"case 0: 预移动 Y 轴 {GetCurrentPreMoveMm():0.###} mm ({GetCurrentPreMoveMm():0.###}/9 格)");
                        SendOrRetryMotorCmd(() =>
                        {
							pcr18Client.MotorControlYAbsolute4Byte(grids);
                            Thread.Sleep(100);
                        }, 100); // 使用 100 作为预移动的 ACK 步骤编号
                        return;
                    }
                    LogHelper.Debug((object)"X 轴正向扫描");
                    SendOrRetryMotorCmd(() =>
                    {
                        pcr18Client.MotorControlForeward();
                        Thread.Sleep(100);
                    }, 0);
                    break;
                case 1:
                    softContinueConsecutive = 0;
                    LogHelper.Debug((object)"Y 轴移动 6 行");
                    // pcr18Client.MotorControlY(3.5);
                    // 改用4字节
                    SendOrRetryMotorCmd(() =>
                    {
                        // 在原有 3.5 格基础上，额外前移 1mm（1/9 格）
						double extraMm = GetYExtraMmForRows();
						pcr18Client.MotorControlYAbsolute4Byte(3.5 + extraMm / 9.0);
                        Thread.Sleep(100);
                        // 移除辅助 0x07 命令，避免 ACK 混淆
                    }, 1);
                    break;
                case 2:
                    softContinueConsecutive = 0;
                    // 若每组预设写入后仍在冷却，则推迟 case2 扫描
                    if (!motorAckPending && DateTime.Now < xAlignGroupCooldownUntil)
                    {
                        LogHelper.Debug($"[X-Align] 组冷却中，推迟 case2 扫描 {(xAlignGroupCooldownUntil - DateTime.Now).TotalMilliseconds:0} ms");
                        return;
                    }
					// 若 X 对齐刚刚锁定了最佳组，则等待冷却时间结束再发送扫描指令
					if (!motorAckPending && DateTime.Now < xAlignPresetCooldownUntil)
					{
						LogHelper.Debug($"[X-Align] 冷却中，推迟 case2 扫描 {(xAlignPresetCooldownUntil - DateTime.Now).TotalMilliseconds:0} ms");
						return;
					}
                    LogHelper.Debug((object)"X 轴反向扫描");
                    SendOrRetryMotorCmd(() =>
                    {
                        pcr18Client.MotorControlReversal();
                        Thread.Sleep(100);
                        // 新增电机控制 2025-05-21
                        pcr18Client.MotorControl2();
                    }, 2);
                    break;
                case 3:
                    softContinueConsecutive = 0;
                    LogHelper.Debug((object)"Y 轴移动 11 行");
                    // 改用4字节
                    SendOrRetryMotorCmd(() =>
                    {
                        // 在原有 7 格基础上，额外前移 1mm（1/9 格）
						double extraMm = GetYExtraMmForRows();
						pcr18Client.MotorControlYAbsolute4Byte(7.0 + extraMm / 9.0);
                        Thread.Sleep(100);
                    }, 3);
                    break;
                case 4:
                    softContinueConsecutive = 0;
                    // 若每组预设写入后仍在冷却，则推迟 case4 扫描
                    if (!motorAckPending && DateTime.Now < xAlignGroupCooldownUntil)
                    {
                        LogHelper.Debug($"[X-Align] 组冷却中，推迟 case4 扫描 {(xAlignGroupCooldownUntil - DateTime.Now).TotalMilliseconds:0} ms");
                        return;
                    }
					// 若 X 对齐刚刚锁定了最佳组，则等待冷却时间结束再发送扫描指令
					if (!motorAckPending && DateTime.Now < xAlignPresetCooldownUntil)
					{
						LogHelper.Debug($"[X-Align] 冷却中，推迟 case4 扫描 {(xAlignPresetCooldownUntil - DateTime.Now).TotalMilliseconds:0} ms");
						return;
					}
                    LogHelper.Debug((object)"X 轴正向扫描");
                    SendOrRetryMotorCmd(() =>
                    {
                        pcr18Client.MotorControlForeward();
                        Thread.Sleep(100);
                        // 新增电机控制 2025-05-21
                        pcr18Client.MotorControl1();
                        pcr18Client.MotorControl3();
                        pcr18Client.MotorControl4();
                        pcr18Client.MotorControl5();
                    }, 4);
                    break;
                case 5:
                    softContinueConsecutive = 0;
                    LogHelper.Debug((object)"X 轴归位");
                    SendOrRetryMotorCmd(() =>
                    {
                        pcr18Client.MoveMotorXHome();
                        Thread.Sleep(100);
                    }, 5);
                    break;
                case 6:
                    LogHelper.Debug((object)"读取FAM数据");
                    GlobalData.LastLightType = 1;
                    GlobalData.LightQueue.Enqueue(1);
                    pcr18Client.ReadFAMData();
                    Thread.Sleep(100);
                    GlobalData.DS.RunMonitorMotorYIndex++;
                    break;
                case 7:
                    LogHelper.Debug((object)"读取Cy5数据");
                    GlobalData.LastLightType = 2;
                    GlobalData.LightQueue.Enqueue(2);
                    pcr18Client.ReadCy5Data();
                    Thread.Sleep(100);
                    GlobalData.DS.RunMonitorMotorYIndex++;
                    break;
                case 8:
                    LogHelper.Debug((object)"读取VIC数据");
                    GlobalData.LastLightType = 3;
                    GlobalData.LightQueue.Enqueue(3);
                    pcr18Client.ReadVICData();
                    Thread.Sleep(100);
                    GlobalData.DS.RunMonitorMotorYIndex++;
                    break;
                case 9:
                    LogHelper.Debug((object)"读取Cy5.5数据");
                    GlobalData.LastLightType = 4;
                    GlobalData.LightQueue.Enqueue(4);
                    pcr18Client.ReadCy55Data();
                    Thread.Sleep(100);
                    GlobalData.DS.RunMonitorMotorYIndex++;
                    break;
                case 10:
                    LogHelper.Debug((object)"读取ROX数据");
                    GlobalData.LastLightType = 5;
                    GlobalData.LightQueue.Enqueue(5);
                    pcr18Client.ReadRoxData();
                    Thread.Sleep(100);
                    GlobalData.DS.RunMonitorMotorYIndex++;
                    break;
                case 11:
                    LogHelper.Debug((object)"读取MOT数据");
                    GlobalData.LastLightType = 6;
                    GlobalData.LightQueue.Enqueue(6);
                    pcr18Client.ReadFittingData();
                    Thread.Sleep(100);
                    GlobalData.DS.RunMonitorMotorYIndex++;
                    break;
                case 12:
                    LogHelper.Debug((object)"Y 轴回到原位 (0)");
                    pcr18Client.MotorControlY(0);
                    Thread.Sleep(100);

                    // 设置为等待中
                    GlobalData.DS.RunMonitorMotorYIndex++;

                    Task.Run(async () =>
                    {
                        // 扫描一轮后，返回五个光数据，电机归位，等待48s，再进行下一轮扫描。
                        await Task.Delay(100);
                        LogHelper.Debug((object)"===>复位 RunMonitorMotorYIndex");
                        GlobalData.DS.RunMonitorMotorYIndex = 0;
                        // 新一轮开始前，预置 Y 轴前移 1mm
                        preYShiftRequest = true;
                        preYShiftDone = false;
                        lightCycle++;

						// 若处于 X 对齐模式：记录本轮峰值，控制候选切换与完成提示（防重复计分）
                        if (xAlignMode)
                        {
                            try
                            {
								if (xAlignLastScoredLightCycle != lightCycle)
								{
									// 防止未采样即计分：只有通道数据长度比上次多，才取最后一个值计分
									double cyclePeak = GetCurrentCyclePeakForChannel(xAlignChannel, out int dataCountNow);
									if (dataCountNow > xAlignLastDataCount[xAlignChannel])
									{
										if (xAlignIdx >= 0 && xAlignIdx < xAlignBest.Length)
										{
											if (cyclePeak > xAlignBest[xAlignIdx])
											{
												xAlignBest[xAlignIdx] = cyclePeak;
											}
										}
										xAlignCyclesDone++;
										xAlignScoredCount++;
										xAlignLastScoredLightCycle = lightCycle;
										xAlignLastDataCount[xAlignChannel] = dataCountNow;
										LogHelper.Debug($"[X-Align] 候选 {xAlignIdx} 第 {xAlignCyclesDone}/{xAlignCyclesPerCandidate} 轮，峰值={cyclePeak:0.##}, 目前最佳={xAlignBest[Math.Max(0,Math.Min(xAlignIdx,xAlignBest.Length-1))]:0.##}, 数据点={dataCountNow}");
									}
									else
									{
										LogHelper.Debug($"[X-Align] 跳过计分：通道数据未增长 (count={dataCountNow}, last={xAlignLastDataCount[xAlignChannel]}), lightCycle={lightCycle}, candidate={xAlignIdx}");
									}
								}
								else
								{
									LogHelper.Debug($"[X-Align] 跳过重复计分，lightCycle={lightCycle}");
								}

                                if (xAlignCyclesDone >= xAlignCyclesPerCandidate)
                                {
                                    xAlignIdx++;
                                    xAlignCyclesDone = 0;
                                    xAlignPresetSentThisCandidate = false; // 进入下一候选前，允许在 case0 前再次下发 5 条命令
									// 切组后复位数据计数，避免上一组的残留数据在新组第一轮被误计分
									for (int iCnt = 0; iCnt < xAlignLastDataCount.Length; iCnt++) xAlignLastDataCount[iCnt] = 0;
									try
									{
										LogHelper.Info($"[X-Align] 切换到候选 {xAlignIdx} / {xAlignLabels.Length - 1}：{xAlignLabels[Math.Max(0, Math.Min(xAlignLabels.Length - 1, xAlignIdx))]}");
									}
									catch { }
                                }

								// 仅当实际计分次数达到“候选数 × 每候选轮次”才进入最终评估与弹窗
								if (xAlignScoredCount >= xAlignLabels.Length * xAlignCyclesPerCandidate && xAlignIdx >= xAlignLabels.Length)
								{
									if (!xAlignResultShown)
									{
										xAlignFinalizePending = true; // 延后到下一轮 case0 空档统一弹窗
										LogHelper.Info("[X-Align] 计分已达阈值，等待轮次结束的空档再弹窗。");
									}
								}
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error(ex);
                            }
                        }
						// 若处于 Y 对齐模式：记录本轮峰值，控制候选切换与完成提示（防重复计分）
						if (yAlignMode)
						{
							try
							{
								if (yAlignLastScoredLightCycle != lightCycle)
								{
									double cyclePeak = GetCurrentCyclePeakForChannel(yAlignChannel);
									if (yAlignIdx >= 0 && yAlignIdx < yAlignBest.Length)
									{
										if (cyclePeak > yAlignBest[yAlignIdx])
										{
											yAlignBest[yAlignIdx] = cyclePeak;
										}
									}
									yAlignCyclesDone++;
									yAlignScoredCount++;
									yAlignLastScoredLightCycle = lightCycle;
									LogHelper.Debug($"[Y-Align] 候选 {yAlignIdx} ({yAlignLabels[yAlignIdx]}) 第 {yAlignCyclesDone}/{yAlignCyclesPerCandidate} 轮，峰值={cyclePeak:0.##}, 目前最佳={yAlignBest[Math.Max(0,Math.Min(yAlignIdx,yAlignBest.Length-1))]:0.##}");
								}
								else
								{
                                    LogHelper.Debug($"[Y-Align] 跳过重复计分，lightCycle={lightCycle}");
								}

								if (yAlignCyclesDone >= yAlignCyclesPerCandidate)
								{
									yAlignIdx++;
									yAlignCyclesDone = 0;
									// 下一候选的预移动将在下一轮 case0 前按 GetCurrentPreMoveMm() 生效
								}

								if (yAlignScoredCount >= yAlignLabels.Length * yAlignCyclesPerCandidate)
								{
									if (!yAlignResultShown)
									{
										int bestIdx = 0;
										double bestVal = double.MinValue;
										for (int k = 0; k < yAlignBest.Length; k++)
										{
											if (yAlignBest[k] > bestVal)
											{
												bestVal = yAlignBest[k];
												bestIdx = k;
											}
										}

										// 应用最佳 Y 预移动和行间额外前移（A/B/C 同步增加同样的 mm）
										currentYPreMoveMm = yAlignCandidatesMm[bestIdx];
										currentYRowExtraMm = yAlignCandidatesMm[bestIdx];

										// 弹窗汇总
										var sb = new StringBuilder();
										sb.AppendLine($"Y 轴最佳预移动：{yAlignLabels[bestIdx]}");
										sb.AppendLine($"通道：{yAlignChannelName}");
										sb.AppendLine($"峰值：{bestVal:0.##}");
										sb.AppendLine();
										sb.AppendLine("各组峰值：");
										for (int iList = 0; iList < yAlignLabels.Length; iList++)
										{
											double v = yAlignBest[iList];
											string vs = (v == double.MinValue) ? "-" : v.ToString("0.##");
											sb.AppendLine($"- {yAlignLabels[iList]}  →  {vs}");
										}
										string msg = sb.ToString();

										LogHelper.Info("[Y-Align] 完成。 " + msg.Replace("\n", " | "));
										Dispatcher.Invoke(() =>
										{
											System.Windows.MessageBox.Show(this, msg, "Y Alignment 结果", MessageBoxButton.OK, MessageBoxImage.Information);
										});

										yAlignMode = false;
										yAlignResultShown = true;
									}
								}
							}
							catch (Exception ex)
							{
								LogHelper.Error(ex);
							}
						}

                        LogHelper.Debug("===>Main 扫描了 {0} 轮", lightCycle);
                    });

                    break;
                default:
                    LogHelper.Debug((object)$"等待中 RunMonitorMotorYIndex 值: {GlobalData.DS.RunMonitorMotorYIndex}");
                    break;
            }
        }

        private void ShowDeivceNotConnectError()
        {
            context.Post(_ =>
            {
                MyMessageBox.CustomMessageBoxResult result =
                MyMessageBox.Show(Properties.Resources.msg_connect_failed,
                MyMessageBox.CustomMessageBoxButton.OK,
                MyMessageBox.CustomMessageBoxIcon.Warning);
            }, null);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F8)
                {
                    StartXAlignmentWithPrompt();
                    e.Handled = true;
                }
				else if (e.Key == Key.F9)
				{
					StartYAlignmentWithPrompt();
					e.Handled = true;
				}
            }
            catch { }
        }

        private void StartXAlignmentWithPrompt()
        {
            string channel = PromptSelectXAlignChannel();
            if (string.IsNullOrWhiteSpace(channel)) return;
            StartXAlignmentTest(channel);
        }

        private string PromptSelectXAlignChannel()
        {
            string[] options = new[] { "FAM", "HEX", "ROX", "Cy5", "Cy5.5" };
            string selected = options[0];

            Window dlg = new Window
            {
                Title = "选择对齐通道",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowInTaskbar = false
            };

            var root = new StackPanel { Margin = new Thickness(20), Orientation = Orientation.Vertical };
            var label = new TextBlock { Text = "请选择需要对齐评估的光源通道：", Margin = new Thickness(0, 0, 0, 12) };
            root.Children.Add(label);

            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(6, 0, 0, 12) };
            foreach (var opt in options)
            {
                var rb = new RadioButton { Content = opt, Margin = new Thickness(0, 6, 0, 0), IsChecked = (opt == selected) };
                rb.Checked += (_, __) => { selected = opt; };
                panel.Children.Add(rb);
            }
            root.Children.Add(panel);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
            var ok = new Button { Content = "开始", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "取消", Width = 80 };
            ok.Click += (_, __) => { dlg.DialogResult = true; };
            cancel.Click += (_, __) => { dlg.DialogResult = false; };
            btns.Children.Add(ok);
            btns.Children.Add(cancel);
            root.Children.Add(btns);

            dlg.Content = root;
            bool? res = dlg.ShowDialog();
            if (res == true) return selected;
            return null;
        }

		private void StartYAlignmentWithPrompt()
		{
			if (xAlignMode)
			{
				LogHelper.Warn("[Y-Align] 请先完成 X 轴对齐。");
				return;
			}
			string channel = PromptSelectXAlignChannel();
			if (string.IsNullOrWhiteSpace(channel)) return;
			StartYAlignmentTest(channel);
		}

        /// <summary>
        /// 发送电机命令并进入 ACK 等待；仅在未等待时发送。
        /// </summary>
        /// <param name="sendAction">发送动作</param>
        /// <param name="step">步骤索引</param>
        private void SendOrRetryMotorCmd(Action sendAction, int step)
        {
            if (!motorAckPending)
            {
                sendAction?.Invoke();
                motorAckPending = true;
                motorAckReceived = false;
                motorAckStep = step;
                motorAckRetries = 0;
                motorAckLastSend = DateTime.Now;
                LogHelper.Debug((object)$"已发送步骤 {step} 指令，等待 ACK...");
            }
            else
            {
                // 正在等待上一条 ACK，本周期不重复发送
                LogHelper.Debug((object)$"仍在等待步骤 {motorAckStep} 的 ACK，跳过发送");
            }
        }

        /// <summary>
        /// 根据步骤索引重发当前命令
        /// </summary>
        /// <param name="step"></param>
        private void ResendMotorCmd(int step)
        {
            switch (step)
            {
                case 0:
                    pcr18Client.MotorControlForeward();
                    Thread.Sleep(100);
                    break;
                case 100:
                    // 预移动 1.375mm（1.375/9 格）
                    pcr18Client.MotorControlYAbsolute4Byte(1.375 / 9.0);
                    Thread.Sleep(100);
                    break;
                case 1:
                    // 3.5 格 + 1mm（1/9 格）
                    pcr18Client.MotorControlYAbsolute4Byte(3.5 + 1.0 / 9.0);
                    Thread.Sleep(100);
                    // 移除辅助 0x07 命令，避免 ACK 混淆
                    break;
                case 2:
                    pcr18Client.MotorControlReversal();
                    Thread.Sleep(100);
                    pcr18Client.MotorControl2();
                    break;
                case 3:
                    // 7 格 + 1mm（1/9 格）
                    pcr18Client.MotorControlYAbsolute4Byte(7.0 + 1.0 / 9.0);
                    Thread.Sleep(100);
                    break;
                case 4:
                    pcr18Client.MotorControlForeward();
                    Thread.Sleep(100);
                    pcr18Client.MotorControl1();
                    pcr18Client.MotorControl3();
                    pcr18Client.MotorControl4();
                    pcr18Client.MotorControl5();
                    break;
                case 5:
                    pcr18Client.MoveMotorXHome();
                    Thread.Sleep(100);
                    break;
                default:
                    break;
            }
        }

        public void ReceiveTestCmd(string hex)
        {
            Pcr18Client_DataReceived(hex);
        }

        public void SendTestCmd(string hex)
        {
            pcr18Client.SendData(hex);
        }

        /// <summary>
        /// 启动 X 轴对齐测试流程。用户需传入通道名：FAM/HEX/ROX/Cy5/Cy5.5
        /// </summary>
        public void StartXAlignmentTest(string channelName)
        {
            xAlignChannelName = channelName?.Trim();
            xAlignChannel = MapChannelName(xAlignChannelName);
            for (int i = 0; i < xAlignBest.Length; i++) xAlignBest[i] = double.MinValue;
			for (int i = 0; i < xAlignLastDataCount.Length; i++) xAlignLastDataCount[i] = 0;
            xAlignIdx = 0;
            xAlignCyclesDone = 0;
            xAlignPresetSentThisCandidate = false;
            xAlignMode = true;
			xAlignResultShown = false;
			xAlignLastScoredLightCycle = -1;
			xAlignScoredCount = 0;
            LogHelper.Info($"[X-Align] 启动。通道={xAlignChannelName}({xAlignChannel})，每候选 {xAlignCyclesPerCandidate} 轮，总候选 {xAlignLabels.Length} 组。");
        }

		/// <summary>
		/// 启动 Y 轴对齐测试：按候选预移动量（mm）跑数据，选择峰值最高的一组
		/// </summary>
		public void StartYAlignmentTest(string channelName)
		{
			yAlignChannelName = channelName?.Trim();
			yAlignChannel = MapChannelName(yAlignChannelName);
			for (int i = 0; i < yAlignBest.Length; i++) yAlignBest[i] = double.MinValue;
			yAlignIdx = 0;
			yAlignCyclesDone = 0;
			yAlignMode = true;
			yAlignResultShown = false;
			yAlignLastScoredLightCycle = -1;
			yAlignScoredCount = 0;
			// 若 X 对齐仍在进行，直接中止 X 状态，优先执行 Y 对齐
			if (xAlignMode || xAlignFinalizePending)
			{
				LogHelper.Warn("[Y-Align] 中止未完成的 X 对齐，立即开始 Y 对齐。");
				xAlignMode = false;
				xAlignFinalizePending = false;
				xAlignResultShown = false;
				xAlignPresetSentThisCandidate = false;
				motorAckPending = false;
			}
			// 确保扫描计时器已开启
			StartReadLightTimer();
			// 新一轮开始前触发一次预移动
			preYShiftRequest = true;
			preYShiftDone = false;
			LogHelper.Info($"[Y-Align] 启动。通道={yAlignChannelName}({yAlignChannel})，候选：{string.Join(", ", yAlignLabels)}；每候选 {yAlignCyclesPerCandidate} 轮。");
		}

        private int MapChannelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            string n = name.Trim().ToUpperInvariant();
            if (n == "FAM") return 0;
            if (n == "CY5") return 1;
            if (n == "HEX" || n == "VIC") return 2;
            if (n == "CY5.5" || n == "CY55") return 3;
            if (n == "ROX") return 4;
            return 0;
        }

        private double GetCurrentCyclePeakForChannel(int ch)
        {
            double max = double.MinValue;
            try
            {
                switch (ch)
                {
                    case 0: // FAM
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataFAMY[t] != null && GlobalData.DataFAMY[t].Count > 0)
                                max = Math.Max(max, GlobalData.DataFAMY[t][GlobalData.DataFAMY[t].Count - 1]);
                        break;
                    case 1: // Cy5
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataCy5Y[t] != null && GlobalData.DataCy5Y[t].Count > 0)
                                max = Math.Max(max, GlobalData.DataCy5Y[t][GlobalData.DataCy5Y[t].Count - 1]);
                        break;
                    case 2: // VIC(HEX)
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataVICY[t] != null && GlobalData.DataVICY[t].Count > 0)
                                max = Math.Max(max, GlobalData.DataVICY[t][GlobalData.DataVICY[t].Count - 1]);
                        break;
                    case 3: // Cy5.5
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataCy55Y[t] != null && GlobalData.DataCy55Y[t].Count > 0)
                                max = Math.Max(max, GlobalData.DataCy55Y[t][GlobalData.DataCy55Y[t].Count - 1]);
                        break;
                    case 4: // ROX
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataROXY[t] != null && GlobalData.DataROXY[t].Count > 0)
                                max = Math.Max(max, GlobalData.DataROXY[t][GlobalData.DataROXY[t].Count - 1]);
                        break;
                }
            }
            catch { }
            if (max == double.MinValue) return 0;
            return max;
        }

        // 带数据长度输出的版本，用于防止“未采样就计分”
        private double GetCurrentCyclePeakForChannel(int ch, out int dataCount)
        {
            double max = double.MinValue;
            int count = 0;
            try
            {
                switch (ch)
                {
                    case 0: // FAM
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataFAMY[t] != null && GlobalData.DataFAMY[t].Count > 0)
                            {
                                int c = GlobalData.DataFAMY[t].Count;
                                count = Math.Max(count, c);
                                max = Math.Max(max, GlobalData.DataFAMY[t][c - 1]);
                            }
                        break;
                    case 1: // Cy5
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataCy5Y[t] != null && GlobalData.DataCy5Y[t].Count > 0)
                            {
                                int c = GlobalData.DataCy5Y[t].Count;
                                count = Math.Max(count, c);
                                max = Math.Max(max, GlobalData.DataCy5Y[t][c - 1]);
                            }
                        break;
                    case 2: // VIC(HEX)
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataVICY[t] != null && GlobalData.DataVICY[t].Count > 0)
                            {
                                int c = GlobalData.DataVICY[t].Count;
                                count = Math.Max(count, c);
                                max = Math.Max(max, GlobalData.DataVICY[t][c - 1]);
                            }
                        break;
                    case 3: // Cy5.5
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataCy55Y[t] != null && GlobalData.DataCy55Y[t].Count > 0)
                            {
                                int c = GlobalData.DataCy55Y[t].Count;
                                count = Math.Max(count, c);
                                max = Math.Max(max, GlobalData.DataCy55Y[t][c - 1]);
                            }
                        break;
                    case 4: // ROX
                        for (int t = 0; t < 18; t++)
                            if (GlobalData.DataROXY[t] != null && GlobalData.DataROXY[t].Count > 0)
                            {
                                int c = GlobalData.DataROXY[t].Count;
                                count = Math.Max(count, c);
                                max = Math.Max(max, GlobalData.DataROXY[t][c - 1]);
                            }
                        break;
                }
            }
            catch { }
            dataCount = count;
            if (max == double.MinValue) return 0;
            return max;
        }

        private StringBuilder packetData = new StringBuilder();
        private bool continuePacket = false;
        private int packetCount = 0;

        /// <summary>
        /// 串口接收数据
        /// </summary>
        /// <param name="hex"></param>
        private void Pcr18Client_DataReceived(string hex)
        {
            try
            {
                LogHelper.Debug("处理<-：{0}", StringUtils.FormatHex(hex));

                if (string.IsNullOrEmpty(hex) || hex.Length < 6)
                {
                    return;
                }

                // 5E 00/01/02 为裂解温度上报（Row A/B/C），报文长度固定5字节：5E [Row] [Hi] [Lo] [00]
                // 例如：5E 01 0F A4 00 -> (0x0F << 8 | 0xA4) / 100 = 40.04 ℃
                // Monitor页 ROW A/B/C 温度帧：严格限定为 5 字节：5E [Row] [Hi] [Lo] [00]
                // 温度计算：((Hi << 8) | Lo) / 100.0
                if ((hex.StartsWith("5E00") || hex.StartsWith("5E01") || hex.StartsWith("5E02"))
                    && hex.Length == 10
                    && hex.EndsWith("00"))
                {
                    try
                    {
                        int row = StringUtils.HexStringToInt(hex.Substring(2, 2)); // 0/1/2 -> A/B/C
                        string tempHex = hex.Substring(4, 4);                        // 第三、第四字节（Hi Lo）
                        int tempRaw = StringUtils.HexStringToInt(tempHex);          // 16进制转十进制

                        // 忽略 0000 的占位/噪声帧，避免把有效温度覆盖为 0℃
                        if (tempRaw == 0)
                        {
                            readComplete = true;
                            return;
                        }

                        // 加10℃偏移后再上报到UI
                        double tempValue = tempRaw / 100.0 + 10.0;

                        var data = new Dictionary<int, double[]>
                        {
                            { row, new double[] { tempValue } }
                        };
                        EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.LysisTempUpdate, Data = data });
                    }
                    catch { }

                    readComplete = true;
                    return;
                }

                // 截取头部
                string head = hex.Substring(0, 6);

                //if (CheckStatus > 0 && (hex.StartsWith("5E910016")
                //    || hex.StartsWith("5E91001C")
                //    || hex.StartsWith("5E810006")))
                //{
                //    CheckStatus++;
                //}

                switch (head)
                {
                    case "5EC300":
                    case "5E8800":   // 预设/LED 等非位移类回执
                    case "5E8100":   // 握手 0x01 的回执，不作为电机移动的 ACK
                    case "5E8700":   // 电机 0x07 的回执（仅此可推进扫描步进）
                    case "5E8D00":   // 增益/预设等回执，不作为电机移动的 ACK
                    case "5E9100":   // 读取 0x11 的回执
                    case "5E9200":   // 读取 0x12 的回执
                        {
                            // 其他命令报文
                            // LogHelper.Debug("收到其他报文: {0}", StringUtils.FormatHex(hex));
                            // 电机相关 ACK 仅接受 0x87；且仅在等待中、状态 0x00 时生效
                            if (head == "5E8700" || head == "5E8100" || head == "5E8D00" || head == "5E9100" || head == "5E9200")
                            {
                                try
                                {
                                    int len = StringUtils.HexStringToInt(hex.Substring(6, 2));
                                    // 长度校验：避免半包/噪声导致越界
                                    if (hex.Length < len * 2 || len < 5)
                                    {
                                        break;
                                    }
                                    string statusHex = hex.Substring((len - 2) * 2, 2);
                                    int status = StringUtils.HexStringToInt(statusHex);
                                    if (motorAckPending && status == 0x00)
                                    {
                                        bool expected = IsExpectedAckForStep(head, motorAckStep);
                                        if (expected)
                                        {
                                            LogHelper.Debug($"匹配期望 ACK: head={head}, step={motorAckStep}, status=0x00");
                                            LogHelper.Debug($"匹配期望 ACK: step={motorAckStep}");
                                            motorAckReceived = true;
                                        }
                                        else
                                        {
                                            // 忽略与当前步骤不匹配的短回执，防止误推进
                                            LogHelper.Debug($"忽略非期望 ACK: head={head}, step={motorAckStep}, status=0x00");
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        break;
                    // 试管插入或拔出
                    case "5EE800":
                        {
                            LogHelper.Debug("收到试管开关状态报文：{0}", StringUtils.FormatHex(hex));
                        // 记录插管事件时间，用于在短时间内放宽 ACK 超时
                        lastTubeEventAt = DateTime.Now;

                            Dictionary<int, bool> keys = pcr18Client.ProcessKeyStatus(hex);
                            foreach (var k in keys)
                            {
                                int tubeIndex = k.Key;
                                bool prevState = GlobalData.DS.PCRKeyStatus[tubeIndex];

                                GlobalData.DS.PCRKeyStatus[tubeIndex] = k.Value;
                                if (k.Value)
                                {
                                    // 新插入的试管
                                    if (!prevState)
                                    {
                                        LogHelper.Debug("===>试管插入: {0}", Tools.GetDockUnit(tubeIndex));

                                        // 开始加热倒计时
                                        StartCountdown(tubeIndex, false);

                                        // 刷新UI（按钮与状态）
                                        SendRefreshUIEvent();
                                    }
                                }
                                else
                                {
                                    TUBE_STATUS statusBefore = GlobalData.GetStatus(tubeIndex);

                                    if (statusBefore == TUBE_STATUS.Heating)
                                    {
                                        pcr18Client.StopHeat((byte)tubeIndex);
                                    }

                                    // 停止倒计时
                                    tubeCountdownTimers[tubeIndex].Stop();

                                    GlobalData.SetStatus(tubeIndex, TUBE_STATUS.NoSample);
                                    // 刷新UI事件
                                    SendRefreshUIEvent();

                                    // 如果是拔出则提示
                                    if (prevState)
                                    {
                                        LogHelper.Debug("===>试管拔出: {0}", Tools.GetDockUnit(tubeIndex));

                                        EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.StopDetectionTime, TubeIndex = tubeIndex });

                                        bool finished = (statusBefore == TUBE_STATUS.LightingCompleted
                                                         || statusBefore == TUBE_STATUS.HeatingCompleted);

                                        if (finished)
                                        {
                                            // 已完成实验：清空数据，但不弹窗
                                            EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.LightClear, TubeIndex = tubeIndex });
                                        }
                                        else
                                        {
                                            // 未完成实验的拔出：清空数据并提示
                                            EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.LightClear, TubeIndex = tubeIndex });

                                            context.Post(_ =>
                                            {
                                                lock (msgShowLock)
                                                {
                                                    if (!msgShow)
                                                    {
                                                        msgShow = true;
                                                        string hole = Tools.GetDockUnit(tubeIndex);
                                                        // 提示消息，跳转
                                                        MyMessageBox.CustomMessageBoxResult result =
                                                        MyMessageBox.Show(hole + Properties.Resources.msg_hole_pull_out,
                                                        MyMessageBox.CustomMessageBoxButton.OK,
                                                        MyMessageBox.CustomMessageBoxIcon.Warning);
                                                        if (result == MyMessageBox.CustomMessageBoxResult.OK)
                                                        {
                                                            msgShow = false;
                                                        }
                                                    }
                                                }
                                            }, null);
                                        }
                                    }
                                }
                            }

                            // 广播一次“试管状态变化”消息，给所有页面做轻量刷新（避免偶发漏刷新）
                            try
                            {
                                NotificationMessage msg = new NotificationMessage { Code = MessageCode.PcrKeyStatus };
                                EventBus.RunMonitor(msg);
                                EventBus.DataAnalyse(msg);
                                EventBus.HeatingDection(msg);
                                EventBus.SampleRegistration(msg);
                            }
                            catch { }
                        }
                        break;
                    // 荧光值
                    case "5E8C00":
                        {
                            int len = pcr18Client.GetDataLength(hex);
                            LogHelper.Debug("收到光数据：字节数 {0}，字符串长度 {1}", len, hex.Length);

                            //Dictionary<int, double[]> monitorData = pcr18Client.ProcessLightData(hex);
                            Dictionary<int, double[]> monitorData = pcr18Client.ProcessLightData96(hex);  // 96 的数据
                            
                            // 去除原镜像并按新规则重映射:
                            // A 行 -> C 行反向: A1->C6, A2->C5, ..., A6->C1
                            // B 行 -> B 行反向: B1->B6, ..., B6->B1
                            // C 行 -> A 行反向: C1->A6, ..., C6->A1
                            Dictionary<int, double[]> remapped = new Dictionary<int, double[]>();
                            foreach (var kv in monitorData)
                            {
                                int idx = kv.Key;
                                int row = idx / 6;   // 0:A, 1:B, 2:C
                                int col = idx % 6;   // 0..5 -> 1..6
                                
                                int mapped;
                                if (row == 0)        // A -> C reversed
                                {
                                    mapped = 2 * 6 + (5 - col);
                                }
                                else if (row == 1)   // B -> B reversed
                                {
                                    mapped = 1 * 6 + (5 - col);
                                }
                                else                 // C -> A reversed
                                {
                                    mapped = 0 * 6 + (5 - col);
                                }
                                remapped[mapped] = kv.Value;
                            }

                            EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.LightUpdate, Data = remapped });
                        }
                        break;
                    // 温度
                    case "5EE300":
                        {
                            Dictionary<int, double[]> tempData = pcr18Client.ProcessTempData(hex);
                            EventBus.HeatingDection(new NotificationMessage { Code = MessageCode.TempUpdate, Data = tempData });
                        }
                        break;
                    // 环境温度
                    case "5E8500":
                        {
                            // 返回：5E 85 00 09 00 0D 03 01 FD
                            // 5E 85 00 09 [Flag_STATUS] [TempHi,TempLo] [Valid] [Chk]
                            // 温度 = ((TempHi << 8) | TempLo) / 100.0
                            // 跳过 Flag_STATUS 1 字节，因此起始索引应为 10
                            int h1 = StringUtils.HexStringToInt(hex.Substring(10, 4));
                            double h1Value = h1 / 100.0;
                            Dictionary<int, double[]> tempData = new Dictionary<int, double[]>
                            {
                                { 0, new double[] { h1Value } }
                            };
                            EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.EnvTempUpdate, Data = tempData });
                        }
                        break;
                    // 热盖温度
                    case "5E8900":
                        {
                            // 返回：5E 89 00 09 1C 40 00 00 4C
                            // 5E 89 00 09 [Flag_STATUS] [TempHi,TempLo] [Valid] [Chk]
                            // 注意：此帧温度字段起始包含状态高字节，实际需从索引8取 4 字节（Status+TempHi）
                            // 例：5E 89 00 09 1A 5A 00 00 64 -> Substring(8,4) == 1A5A -> 0x1A5A / 100 = 67.46℃
                            int h1 = StringUtils.HexStringToInt(hex.Substring(8, 4));
                            double h1Value = h1 / 100.0;
                            Dictionary<int, double[]> tempData = new Dictionary<int, double[]>
                            {
                                { 0, new double[] { h1Value } }
                            };
                            EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.HotCoverTempUpdate, Data = tempData });
                        }
                        break;
                    default:
                        { LogHelper.Debug("其他数据：{0}", StringUtils.FormatHex(hex)); }

                        break;
                }

                readComplete = true;
            }
            catch (Exception ex)
            {
                LogHelper.Debug((object)(ex.Message + ex.StackTrace));
            }
        }

        private int CheckStatus = 0;

        /// <summary>
        /// 打开设备串口
        /// </summary>
        private void OpenInstrument()
        {
            try
            {
                pcr18Client.PortName = ConfigParam.DevicePort;
                pcr18Client.BaudRate = BaudRates.BR_9600;
                if (!pcr18Client.Open())
                {
                    NotFoundDeviceNotice();

                    return;
                }
                LogHelper.Debug((object)("打开端口成功：" + ConfigParam.DevicePort));

                CheckStatus = 0;
                int waitSeconds = 5 * 1000;
                //while (waitSeconds > 0 && CheckStatus < 8)
                //{
                //    if (CheckStatus == 0)
                //    {
                //        LogHelper.Debug("1.发送读取设备型号：5E 11 00 09 00 00 20 10 A8");
                //        pcr18Client.DevicePCRReadDeviceID();
                //        CheckStatus++;
                //    }
                //    else if (CheckStatus == 2)
                //    {
                //        LogHelper.Debug("2.发送读取仪器型号：5E 11 00 09 00 00 00 16 8E");
                //        pcr18Client.DevicePCRReadInstrumentID();
                //        CheckStatus++;
                //    }
                //    else if (CheckStatus == 4)
                //    {
                //        LogHelper.Debug("3.发送读取SN：5E 11 00 09 00 00 10 10 98 ");
                //        pcr18Client.DevicePCRReadSN();
                //        CheckStatus++;
                //    }
                //    else if (CheckStatus == 6)
                //    {
                //        LogHelper.Debug("4.发送握手指令：5E 01 00 05 64");
                //        pcr18Client.DevicePCRHandshake();
                //        CheckStatus++;
                //    }
                //    Thread.Sleep(1);
                //    waitSeconds--;
                //}

                LogHelper.Debug((object)"1.发送读取设备型号：5E 11 00 09 00 00 20 10 A8");
                pcr18Client.DevicePCRReadDeviceID();
                CheckStatus++;

                LogHelper.Debug((object)"2.发送读取仪器型号：5E 11 00 09 00 00 00 16 8E");
                pcr18Client.DevicePCRReadInstrumentID();
                CheckStatus++;

                LogHelper.Debug((object)"3.发送读取SN：5E 11 00 09 00 00 10 10 98 ");
                pcr18Client.DevicePCRReadSN();
                CheckStatus++;

                LogHelper.Debug((object)"4.发送握手指令：5E 01 00 05 64");
                pcr18Client.DevicePCRHandshake();
                CheckStatus++;

                // 这个命令，导致下位机无响应
                LogHelper.Debug((object)"5.发送循环读10次：5E 11 00 09 00 开头");
                pcr18Client.DeviceTenTime();
                Thread.Sleep(100);

                LogHelper.Debug((object)"6.发送Nothing：5E 11 00 09 00 09 BA 46 81");
                pcr18Client.DevicePCRNothing();
                Thread.Sleep(100);

                LogHelper.Debug((object)"7.发送读取结构化参数：5E 11 00 09 00 20 00 F9 91");
                pcr18Client.DeviceStructPara();
                Thread.Sleep(100);

                // 获取试管开关状态
                if (!IsDebug)
                {
                    pcr18Client.ReadHeatKeyStatus();
                    Thread.Sleep(50);
                }

                if (CheckStatus < 4)
                {
                    LogHelper.Debug((object)"握手失败");
                    NotFoundDeviceNotice();
                    return;
                }

                // 成功
                GlobalData.DS.PCRStatus = true;
            }
            catch (Exception ex)
            {
                LogHelper.Debug((object)ex.Message);
            }

            context.Post(_ =>
            {
                SHowMainWin();

            }, null);
        }

        private void NotFoundDeviceNotice()
        {
            context.Post(_ =>
            {
                // 未检测到仪器
                loading.txtCheck.Text = Properties.Resources.msg_no_instrument_detected;

                // 提示消息，跳转
                MyMessageBox.CustomMessageBoxResult result =
                MyMessageBox.Show(Properties.Resources.msg_connect_failed,
                MyMessageBox.CustomMessageBoxButton.OK,
                MyMessageBox.CustomMessageBoxIcon.Warning);
                if (result == MyMessageBox.CustomMessageBoxResult.OK)
                {
                    SHowMainWin();
                }

            }, null);
        }

        private void SHowMainWin()
        {
            loading.Visibility = Visibility.Hidden;
            mainGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 异步检测设备连接
        /// </summary>
        private void CheckInstrumentTask()
        {
            loading.txtCheck.Text = Properties.Resources.msg_identify_instrument;
            Task task = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(300);
                OpenInstrument();
            });
        }

        private void NavPage(BasePage page)
        {
            ps.NavigatePage(page);
        }

        private int SelectedTab = 0;

        /// <summary>
        /// 菜单点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Tab1_ClickEventTick(LeftTab sender, bool click)
        {
            SelectedTab = sender.Index;
            foreach (FrameworkElement element in leftMenu.Children)
            {
                var tab = (element as DockPanel).Children[0] as LeftTab;
                if (tab.Index != sender.Index)
                {
                    tab.Reset();
                }
            }

            if (!click)
            {
                btnImport.Visibility = Visibility.Collapsed;
                sender.Click = true;
                switch (sender.Index)
                {
                    case 0:
                        NavPage(sampleRegistrationPage);
                        gAnalysisParameters.Visibility = Visibility.Collapsed;
                        break;
                    case 1:
                        NavPage(runMonitorPage);
                        gAnalysisParameters.Visibility = Visibility.Collapsed;
                        break;
                    case 2:
                        btnImport.Visibility = Visibility.Visible;
                        NavPage(dataAnalysePage);
                        gAnalysisParameters.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        NavPage(heatingDetectionPage);
                        gAnalysisParameters.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            TracePoint("Close_Click");
            // 防止在拖拽逻辑介入时弹窗焦点丢失
            _maybeDragFromTitle = false;
            _isDraggingFromTitle = false;
            _isDraggingFromEdge = false;
            _suspendAutoMaximize = false;
            MyMessageBox.CustomMessageBoxResult result =
                MyMessageBox.Show(Properties.Resources.logout_confirm,
                MyMessageBox.CustomMessageBoxButton.YesNo,
                MyMessageBox.CustomMessageBoxIcon.Question);
            if (result == MyMessageBox.CustomMessageBoxResult.Yes)
            {
                CloseWin();
            }
            else
            {
                try { this.WindowState = WindowState.Maximized; } catch { }
            }
            try { e.Handled = true; } catch { }
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            TracePoint("Minimize_Click");
            this.WindowState = WindowState.Minimized;
            try { e.Handled = true; } catch { }
        }

        /// <summary>
        /// 顶部菜单点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopTab_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button).Tag.ToString();
            switch (tag)
            {
                case "experiment":
                    experimentGrid.Visibility = Visibility.Visible;
                    settingGrid.Visibility = Visibility.Hidden;
                    if (SelectedTab == 2)
                    {
                        btnImport.Visibility = Visibility.Visible;
                        gAnalysisParameters.Visibility = Visibility.Visible;
                    }
                    else { gAnalysisParameters.Visibility = Visibility.Collapsed; }
                    break;
                case "setting":
                    experimentGrid.Visibility = Visibility.Hidden;
                    settingGrid.Visibility = Visibility.Visible;
                    btnImport.Visibility = Visibility.Collapsed;
                    gAnalysisParameters.Visibility = Visibility.Collapsed;
                    break;
                case "export":
                    {
                        // 导出完成检测，有光数据的
                        popExport.IsOpen = true;

                    }
                    break;
                case "import":
                    {
                        // 导入，数据分析
                        dataAnalysePage.OpenDataFileDialog();
                    }
                    break;
            }
        }

        private void cmbAnalysisParametersTop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var combo = sender as ComboBox;
                if (combo == null) return;
                var item = combo.SelectedItem as ComboBoxItem;
                string content = item?.Content?.ToString() ?? string.Empty;

                if (string.Equals(content, "Turbility", StringComparison.OrdinalIgnoreCase))
                {
                    var win = new PageUi.TurbilityWindow();
                    win.Owner = this;
                    var result = win.ShowDialog();
                    Util.LogHelper.Info("Turbility dialog result={0}", result);
                    if (result == true)
                    {
                        // Map dye order: 1..5 => FAM, HEX(VIC), ROX, Cy5, Cy5.5
                        // UI rows are 1..5 in that order; save to GlobalData.TurbidityEnabled
                        Common.GlobalData.TurbidityEnabled[0] = win.UseAdjust[0]; // FAM
                        Common.GlobalData.TurbidityEnabled[1] = win.UseAdjust[1]; // HEX(VIC)
                        Common.GlobalData.TurbidityEnabled[2] = win.UseAdjust[2]; // ROX
                        Common.GlobalData.TurbidityEnabled[3] = win.UseAdjust[3]; // Cy5
                        Common.GlobalData.TurbidityEnabled[4] = win.UseAdjust[4]; // Cy5.5
                        // 保存比例：从窗体读取 0.1/0.2
                        Common.GlobalData.TurbidityAdjustScale[0] = win.AdjustScale[0];
                        Common.GlobalData.TurbidityAdjustScale[1] = win.AdjustScale[1];
                        Common.GlobalData.TurbidityAdjustScale[2] = win.AdjustScale[2];
                        Common.GlobalData.TurbidityAdjustScale[3] = win.AdjustScale[3];
                        Common.GlobalData.TurbidityAdjustScale[4] = win.AdjustScale[4];

                        // 立即刷新数据分析视图：按当前曲线类型重绘
                        try
                        {
                            Util.LogHelper.Info("Turbility Saved: FAM={0}, HEX={1}, ROX={2}, Cy5={3}, Cy5.5={4}",
                                Common.GlobalData.TurbidityEnabled[0],
                                Common.GlobalData.TurbidityEnabled[1],
                                Common.GlobalData.TurbidityEnabled[2],
                                Common.GlobalData.TurbidityEnabled[3],
                                Common.GlobalData.TurbidityEnabled[4]);
                            dataAnalysePage?.Dispatcher.Invoke(() =>
                            {
                                Util.LogHelper.Info("ForceRefreshCurves() start");
                                dataAnalysePage.ForceRefreshCurves();
                                Util.LogHelper.Info("ForceRefreshCurves() done");
                            });
                        }
                        catch { }
                    }
                    // reset to placeholder after closing
                    combo.SelectedIndex = -1;
                }
                else if (!string.IsNullOrEmpty(content))
                {
                    if (string.Equals(content, "Crosstalk correction", StringComparison.OrdinalIgnoreCase))
                    {
                        var win = new PageUi.CrosstalkWindow();
                        win.Owner = this;
                        var ok = win.ShowDialog();
                        Util.LogHelper.Info("Crosstalk dialog result={0}", ok);
                        // 保存后刷新曲线
                        if (ok == true)
                        {
                            dataAnalysePage?.Dispatcher.Invoke(() =>
                            {
                                dataAnalysePage.ForceRefreshCurves();
                            });
                        }
                    }
                    else if (string.Equals(content, "Basic Parameters", StringComparison.OrdinalIgnoreCase))
                    {
                        var win = new PageUi.BasicParametersWindow();
                        win.Owner = this;
                        var ok = win.ShowDialog();
                        Util.LogHelper.Info("BasicParameters dialog result={0}", ok);
                        if (ok == true)
                        {
                            try
                            {
                                dataAnalysePage?.Dispatcher.Invoke(() =>
                                {
                                    dataAnalysePage.ForceRefreshCurves();
                                });
                            }
                            catch { }
                        }
                    }
                    else if (string.Equals(content, "Filtr Parameters", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(content, "Filter Parameters", StringComparison.OrdinalIgnoreCase))
                    {
                        var win = new PageUi.FilterParametersWindow();
                        win.Owner = this;
                        var ok = win.ShowDialog();
                        Util.LogHelper.Info("FilterParameters dialog result={0}", ok);
                        if (ok == true)
                        {
                            try
                            {
                                Common.GlobalData.FilterParams.MedianWindow = win.MedianWindow;
                                Common.GlobalData.FilterParams.SmoothPasses = win.SmoothPasses;
                                Common.GlobalData.FilterParams.SmoothForwardM = win.SmoothForwardM;
                                Common.GlobalData.FilterParams.SmoothBackwardN = win.SmoothBackwardN;
                                Common.GlobalData.FilterParams.CtThreshold = win.CtThreshold;

                                dataAnalysePage?.Dispatcher.Invoke(() =>
                                {
                                    dataAnalysePage.ForceRefreshCurves();
                                });
                            }
                            catch { }
                        }
                    }
                    // reset selection back to placeholder
                    combo.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Util.LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 点击导出菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as MenuItem).Tag.ToString();
            switch (tag)
            {
                case "select":
                    {
                        SelectTubeWin tubeWin = new SelectTubeWin();
                        tubeWin.Owner = this;
                        if (tubeWin.ShowDialog() == true)
                        {
                            // 获取输入数据
                            List<int> list = tubeWin.TubeSelected();
                            Console.WriteLine("选择了：" + list.Count);
                            ExportDialog(list);
                        }
                    }
                    break;
                case "all":
                    {
                        List<int> list = new List<int>();
                        for (int i = 0; i < 18; i++)
                        {
                            if (GlobalData.GetStatus(i) == TUBE_STATUS.LightingCompleted)
                            {
                                list.Add(i);
                            }
                        }
                        ExportDialog(list);
                    }
                    break;
            }
        }

        /// <summary>
        /// "导出"失去焦点时发生事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportPopup_LostFocus(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// "导出"隐藏时发生事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportPopup_Closed(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// "导出"显示时发生事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportPopup_Opened(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 保存患者信息
        /// </summary>
        /// <param name="tubeIndex"></param>
        private void SavePatientInfo(int tubeIndex)
        {
            string patientId = GlobalData.DS.HeatPatientID[tubeIndex];
            if (string.IsNullOrEmpty(patientId))
            {
                return;
            }

            PatientDAL patientDAL = new PatientDAL();
            Patient patient = patientDAL.FindByPatientId(patientId);
            if (patient == null)
            {
                patient = new Patient()
                {
                    PatientId = patientId,
                    Name = "",
                    Gender = 0,
                    Birthday = "",
                    Address = "",
                    Phone = "",
                    CreateTime = DateTime.Now,
                };

                patientDAL.Insert(patient);
            }
        }

        private void TableDataColumn(string channel, double ct, DataTable dataTable)
        {
            // Origin
            dataTable.Columns.Add(new DataColumn(string.Format("{0} (CT={1})", channel, ct))
            {
                DataType = typeof(int)
            });

            // Filter
            dataTable.Columns.Add(new DataColumn(string.Format("{0} (Filter)", channel))
            {
                DataType = typeof(int)
            });

            // DeltaRn
            dataTable.Columns.Add(new DataColumn(string.Format("{0} (DeltaRn)", channel))
            {
                DataType = typeof(int)
            });

            // Normalize
            dataTable.Columns.Add(new DataColumn(string.Format("{0} (Normalize)", channel))
            {
                DataType = typeof(double)
            });
        }

        /// <summary>
        /// 弹出文件选择框，导出数据
        /// </summary>
        /// <param name="tubeList"></param>
        private void ExportDialog(List<int> tubeList)
        {
            if (tubeList == null || tubeList.Count == 0)
            {
                return;
            }

            // 默认使用第一个选中试管的信息拼接文件名
            int firstIndex = tubeList[0];
            string ts = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
            string dock = Tools.GetDockUnit(firstIndex);
            string sampleId = Tools.SanitizeFileName(GlobalData.DS.HeatSampleID[firstIndex] ?? "");
            string patientId = Tools.SanitizeFileName(GlobalData.DS.HeatPatientID[firstIndex] ?? "");
            int typeId = GlobalData.DS.HeatSampleType[firstIndex];
            string typeText = VarDef.SampleType.ContainsKey(typeId)
                ? Tools.SanitizeFileName(VarDef.SampleType[typeId][0])
                : "";
            string extra = string.Join("-", new[] { sampleId, patientId, typeText }.Where(s => !string.IsNullOrWhiteSpace(s)));

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = string.IsNullOrWhiteSpace(extra) ? ($"{dock}-{ts}.xlsx") : ($"{dock}-{ts}-{extra}.xlsx"),
                Filter = "Excel (*.xlsx)|*.xlsx"
            };

            ConfigCache configCache = CacheFileUtil.Read();
            if (configCache != null && !string.IsNullOrEmpty(configCache.DataPath))
            {
                saveFileDialog.InitialDirectory = configCache.DataPath;
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                ExportLightData(tubeList, filePath);
            }
        }

        /// <summary>
        /// 导出光数据
        /// </summary>
        private void ExportLightData(List<int> tubeList, string filePath)
        {
            if (tubeList == null || tubeList.Count == 0)
            {
                return;
            }

            List<DataTable> dataTables = new List<DataTable>();
            List<string> sheetNames = new List<string>();

            // 保存处理，写入文件
            foreach (int i in tubeList)
            {
                if (GlobalData.GetStatus(i) == TUBE_STATUS.LightingCompleted)
                {
                    string dockUnit = Tools.GetDockUnit(i);
                    sheetNames.Add(dockUnit);

                    DataTable dataTable = new DataTable();

                    // ======= 表头 START ============
                    // Cycle
                    dataTable.Columns.Add(new DataColumn("Cycle")
                    {
                        DataType = typeof(int)
                    });

                    // DeltaRn
                    var (deltaFAM, deltaCy5, deltaVIC, deltaCy55, deltaROX) =
                        PcrAlgorigthm.DeltaRn(i);

                    // FAM
                    double ctFAM = PcrAlgorigthm.GetCt(i, 0);
                    TableDataColumn("FAM", ctFAM, dataTable);

                    // Cy5
                    double ctCy5 = PcrAlgorigthm.GetCt(i, 1);
                    TableDataColumn("Cy5", ctCy5, dataTable);

                    // VIC
                    double ctVIC = PcrAlgorigthm.GetCt(i, 2);
                    TableDataColumn("VIC", ctVIC, dataTable);

                    // Cy5.5
                    double ctCy55 = PcrAlgorigthm.GetCt(i, 3);
                    TableDataColumn("Cy5.5", ctCy55, dataTable);

                    // ROX
                    double ctROX = PcrAlgorigthm.GetCt(i, 4);
                    TableDataColumn("ROX", ctROX, dataTable);

                    // MOT， 列 21
                    dataTable.Columns.Add(new DataColumn(string.Format("{0}", "MOT"))
                    {
                        DataType = typeof(double)
                    });

                    // 用户信息， 列22
                    dataTable.Columns.Add(new DataColumn(string.Format("{0}", "INFO"))
                    {
                        DataType = typeof(string)
                    });

                    // ======= 表头 END ============

                    // Normalized
                    var (normalizedFAM, normalizedCy5, normalizedVIC, normalizedCy55, normalizedROX) =
                        PcrAlgorigthm.Normalized(i);

                    // 原始扩增
                    var (filterFAM, filterCy5, filterVIC, filterCy55, filterROX)
                        = PcrAlgorigthm.Amplify(i);

                    // ==== DATA ====
                    for (int j = 0; j < GlobalData.DataFAMX[i].Count; j++)
                    {
                        string infoData = "";
                        if (j == 0)
                        {
                            infoData = GlobalData.DS.HeatPatientID[i];
                        }
                        else if (j == 1)
                        {
                            infoData = GlobalData.DS.HeatSampleType[i].ToString();
                        }

                        dataTable.Rows.Add(new object[]
                        {
                                j+1,
                                (int)GlobalData.DataFAMY[i][j],
                                (int)filterFAM[j],
                                (int)deltaFAM[j],
                                normalizedFAM[j],

                                (int)GlobalData.DataCy5Y[i][j],
                                (int)filterCy5[j],
                                (int)deltaCy5[j],
                                normalizedCy5[j],

                                (int)GlobalData.DataVICY[i][j],
                                (int)filterVIC[j],
                                (int)deltaVIC[j],
                                normalizedVIC[j],

                                (int)GlobalData.DataCy55Y[i][j],
                                (int)filterCy55[j],
                                (int)deltaCy55[j],
                                normalizedCy55[j],

                                (int)GlobalData.DataROXY[i][j],
                                (int)filterROX[j],
                                (int)deltaROX[j],
                                normalizedROX[j],

                                (int)GlobalData.DataMOTY[i][j],

                                infoData,
                        });
                    }

                    dataTables.Add(dataTable);
                }
            }

            // 导出excel
            ExcelHelper.DataTableToExecl(filePath, dataTables, sheetNames);
        }

        private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TestWin testWin = new TestWin(this);
            testWin.Owner = this;
            testWin.Show();
        }
    }
}
