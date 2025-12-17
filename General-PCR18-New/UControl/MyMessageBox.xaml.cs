using System;
using System.Windows;

namespace General_PCR18.UControl
{
    /// <summary>
    /// Interaction logic for MyMessageBox.xaml
    /// </summary>
    public partial class MyMessageBox : Window
    {
        /// <summary>
        /// 显示的内容
        /// </summary>
        public string MessageBoxText { get; set; }
        /// <summary>
        /// 显示的图片
        /// </summary>
        public string ImagePath { get; set; }
        /// <summary>
        /// 控制显示 OK 按钮
        /// </summary>
        public Visibility OkButtonVisibility { get; set; }
        /// <summary>
        /// 控制显示 Cacncel 按钮
        /// </summary>
        public Visibility CancelButtonVisibility { get; set; }
        /// <summary>
        /// 控制显示 Yes 按钮
        /// </summary>
        public Visibility YesButtonVisibility { get; set; }
        /// <summary>
        /// 控制显示 No 按钮
        /// </summary>
        public Visibility NoButtonVisibility { get; set; }
        /// <summary>
        /// 消息框的返回值
        /// </summary>
        public CustomMessageBoxResult Result { get; set; }

        public MyMessageBox()
        {
            InitializeComponent();

            this.DataContext = this;

            OkButtonVisibility = Visibility.Collapsed;
            CancelButtonVisibility = Visibility.Collapsed;
            YesButtonVisibility = Visibility.Collapsed;
            NoButtonVisibility = Visibility.Collapsed;

            Result = CustomMessageBoxResult.None;
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.OK;
            this.Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Yes;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.No;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Cancel;
            this.Close();
        }

        public static CustomMessageBoxResult Show(string messageBoxText, CustomMessageBoxButton messageBoxButton, CustomMessageBoxIcon messageBoxImage)
        {
            MyMessageBox window = new MyMessageBox
            {
                Owner = Application.Current.MainWindow,
                Topmost = true,
                MessageBoxText = messageBoxText
            };

            switch (messageBoxImage)
            {
                case CustomMessageBoxIcon.Question:
                    window.ImagePath = @"/Images/querytip.png";
                    break;
                case CustomMessageBoxIcon.Error:
                    window.ImagePath = @"/Images/warningtip.png";
                    break;
                case CustomMessageBoxIcon.Warning:
                    window.ImagePath = @"/Images/warningtip.png";
                    break;
            }
            switch (messageBoxButton)
            {
                case CustomMessageBoxButton.OK:
                    window.OkButtonVisibility = Visibility.Visible;
                    break;
                case CustomMessageBoxButton.OKCancel:
                    window.OkButtonVisibility = Visibility.Visible;
                    window.CancelButtonVisibility = Visibility.Visible;
                    break;
                case CustomMessageBoxButton.YesNo:
                    window.YesButtonVisibility = Visibility.Visible;
                    window.NoButtonVisibility = Visibility.Visible;
                    break;
                case CustomMessageBoxButton.YesNoCancel:
                    window.YesButtonVisibility = Visibility.Visible;
                    window.NoButtonVisibility = Visibility.Visible;
                    window.CancelButtonVisibility = Visibility.Visible;
                    break;
                default:
                    window.OkButtonVisibility = Visibility.Visible;
                    break;
            }

            window.ShowDialog();
            return window.Result;
        }

        public static CustomMessageBoxResult Show(string messageBoxText)
        {
            return Show(messageBoxText, CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
        }

        /// <summary>
        /// 显示按钮类型
        /// </summary>
        public enum CustomMessageBoxButton
        {
            OK = 0,
            OKCancel = 1,
            YesNo = 2,
            YesNoCancel = 3
        }
        /// <summary>
        /// 消息框的返回值
        /// </summary>
        public enum CustomMessageBoxResult
        {
            //用户直接关闭了消息窗口
            None = 0,
            //用户点击确定按钮
            OK = 1,
            //用户点击取消按钮
            Cancel = 2,
            //用户点击是按钮
            Yes = 3,
            //用户点击否按钮
            No = 4
        }
        /// <summary>
        /// 图标类型
        /// </summary>
        public enum CustomMessageBoxIcon
        {
            None = 0,
            Error = 1,
            Question = 2,
            Warning = 3
        }
    }
}
