using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace General_PCR18.UControl
{
    /// <summary>
    /// Interaction logic for SampleUC.xaml
    /// </summary>
    public partial class SampleUC : UserControl
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int Index { get; set; }

        public bool Click { get; set; }

        public delegate void ClickEventHandler(SampleUC sender, bool click);
        public event ClickEventHandler ClickEventTick;

        // 开始按钮
        public delegate void StartClickEventHandler(SampleUC sender, bool click);
        public event StartClickEventHandler StartClickEventTick;

        // 倒计时
        private DispatcherTimer timer;
        private ProcessCount processCount;

        private class ProcessCount
        {
            private int TotalSecond;
            public ProcessCount(int totalSecond)
            {
                TotalSecond = totalSecond;
            }

            public void SetTotalSecond(int totalSecond)
            {
                TotalSecond = totalSecond;
            }

            public bool ProcessCountDown()
            {
                if (TotalSecond == 0)
                    return false;
                else
                {
                    TotalSecond--;
                    return true;
                }
            }

            public string GetHour()
            {
                return string.Format("{0:D2}", (TotalSecond / 3600));
            }

            public string GetMinute()
            {
                return string.Format("{0:D2}", (TotalSecond % 3600) / 60);
            }

            public string GetSecond()
            {
                return string.Format("{0:D2}", TotalSecond % 60);
            }

            public int GetRemainingMinutesCeil()
            {
                return (int)Math.Ceiling(TotalSecond / 60.0);
            }
        }

        public SampleUC()
        {
            InitializeComponent();

            processCount = new ProcessCount(60 * 60);

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1); // 1s
            timer.Tick += Timer_Tick;

            timerBox.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (processCount.ProcessCountDown())
            {
                MinuteArea.Content = processCount.GetRemainingMinutesCeil().ToString();
            }
            else
            {
                StopTimer();
            }
        }

        /// <summary>
        /// 设置定时间隔
        /// </summary>
        /// <param name="timeSpan"></param>
        public void SetTimeSpan(TimeSpan timeSpan)
        {
            timer.Interval = timeSpan;
        }

        /// <summary>
        /// 设置总时长
        /// </summary>
        /// <param name="seconds"></param>
        public void SetTotalSecond(int seconds)
        {
            processCount.SetTotalSecond(seconds);
        }

        /// <summary>
        /// 开始计时
        /// </summary>
        public void StartTimer(string text = "Time Remaining:")
        {
            StopTimer();
            Thread.Sleep(100);

            MinuteArea.Content = processCount.GetRemainingMinutesCeil().ToString();

            countdownTitle.Content = "";
            timer.Start();
            timerBox.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        public void StopTimer()
        {
            timer.Stop();
            timerBox.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 边框颜色
        /// </summary>
        public SolidColorBrush BorderColor
        {
            get { return (SolidColorBrush)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }
        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register("BorderColor", typeof(SolidColorBrush), typeof(SampleUC),
                new PropertyMetadata(null, new PropertyChangedCallback(UpdateBorderColor)));

        private static void UpdateBorderColor(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SampleUC)d;
            ctrl.borderGrid.BorderBrush = (SolidColorBrush)e.NewValue;
        }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public SolidColorBrush BackgroundColor
        {
            get { return (SolidColorBrush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(SolidColorBrush), typeof(SampleUC),
                new PropertyMetadata(null, new PropertyChangedCallback(UpdateBackgroundColorProperty)));

        private static void UpdateBackgroundColorProperty(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SampleUC)d;
            ctrl.boxGrid.Background = (SolidColorBrush)e.NewValue;
        }

        /// <summary>
        /// 进度, 0 - 1
        /// </summary>
        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(SampleUC),
                new PropertyMetadata(0.0, new PropertyChangedCallback(UpdateOpacityMask)));

        /// <summary>
        /// 间隔高度
        /// </summary>
        public GridLength SeparateHeight
        {
            get { return (GridLength)GetValue(SeparateHeightProperty); }
            set { SetValue(SeparateHeightProperty, value); }
        }
        public static readonly DependencyProperty SeparateHeightProperty =
            DependencyProperty.Register("SeparateHeight", typeof(GridLength), typeof(SampleUC),
                new PropertyMetadata(new GridLength(20)));  // 默认为20
        

        /// <summary>
        /// 样本类型, HPV RNA DNA
        /// </summary>
        public string SampleTypeText
        {
            get { return (string)GetValue(SampleTypeProperty); }
            set { SetValue(SampleTypeProperty, value); }
        }
        public static readonly DependencyProperty SampleTypeProperty =
            DependencyProperty.Register("SampleType", typeof(string), typeof(SampleUC),
                new PropertyMetadata("UN", new PropertyChangedCallback(UpdateSampleTypeProperty)));

        private static void UpdateSampleTypeProperty(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SampleUC)d;
            string val = e.NewValue as string;
            if ("NONE" == val)
            {
                val = "";
            }
            ctrl.txtSampleType.Text = val;
        }

        /// <summary>
        /// 阳性标志
        /// </summary>
        public string PositiveMarker
        {
            get { return (string)GetValue(PositiveMarkerProperty); }
            set { SetValue(PositiveMarkerProperty, value); }
        }
        public static readonly DependencyProperty PositiveMarkerProperty =
            DependencyProperty.Register("PositiveMarker", typeof(string), typeof(SampleUC),
                new PropertyMetadata(""));

        /// <summary>
        /// 患者ID
        /// </summary>
        public string PatientId
        {
            get { return (string)GetValue(PatientIdProperty); }
            set { SetValue(PatientIdProperty, value); }
        }
        public static readonly DependencyProperty PatientIdProperty =
            DependencyProperty.Register("PatientId", typeof(string), typeof(SampleUC),
                new PropertyMetadata(""));

        /// <summary>
        /// 是否显示按钮
        /// </summary>
        public string ButtonDisplay
        {
            get { return (string)GetValue(ButtonDisplayProperty); }
            set { SetValue(ButtonDisplayProperty, value); }
        }
        public static readonly DependencyProperty ButtonDisplayProperty =
            DependencyProperty.Register("ButtonDisplay", typeof(string), typeof(SampleUC),
                new PropertyMetadata("Visible"));

        /// <summary>
        /// 是否显示样本类型
        /// </summary>
        public string SampleTypeDisplay
        {
            get { return (string)GetValue(SampleTypeDisplayProperty); }
            set { SetValue(SampleTypeDisplayProperty, value); }
        }
        public static readonly DependencyProperty SampleTypeDisplayProperty =
            DependencyProperty.Register("SampleTypeDisplay", typeof(string), typeof(SampleUC),
                new PropertyMetadata("Visible"));

        /// <summary>
        /// 按钮状态
        /// </summary>
        public bool ButtonEnabled
        {
            get { return (bool)GetValue(ButtonEnabledProperty); }
            set { SetValue(ButtonEnabledProperty, value); }
        }
        public static readonly DependencyProperty ButtonEnabledProperty =
            DependencyProperty.Register("ButtonEnabled", typeof(bool), typeof(SampleUC),
                new PropertyMetadata(false));

        /// <summary>
        /// 按钮文字
        /// </summary>
        public string ButtonText
        {
            get { return (string)GetValue(ButtonTextProperty); }
            set { SetValue(ButtonTextProperty, value); }
        }
        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register("ButtonText", typeof(string), typeof(SampleUC),
                new PropertyMetadata("No Sample"));

        private void Click_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            ClickEventTick?.Invoke(this, Click);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            StartClickEventTick?.Invoke(this, Click);
        }

        public void TriggerCustomEvent()
        {
            ClickEventTick?.Invoke(this, Click);
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void UpdateOpacityMask(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (SampleUC)d;

            double progress = (double)e.NewValue;

            if (progress > 1) { progress = 1; }

            // 设置背景颜色为单一颜色
            ctrl.progressGrid.Background = new SolidColorBrush(Color.FromRgb(6, 145, 157));

            // 设置 OpacityMask
            var opacityMask = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };

            opacityMask.GradientStops.Add(new GradientStop(Colors.Black, 0));
            opacityMask.GradientStops.Add(new GradientStop(Colors.Black, progress));
            opacityMask.GradientStops.Add(new GradientStop(Colors.Transparent, progress));
            opacityMask.GradientStops.Add(new GradientStop(Colors.Transparent, 1));

            ctrl.progressGrid.OpacityMask = opacityMask;
        }
    }
}
