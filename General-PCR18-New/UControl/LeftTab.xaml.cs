using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace General_PCR18.UControl
{
    /// <summary>
    /// Interaction logic for LeftTab.xaml
    /// </summary>
    public partial class LeftTab : UserControl
    {
        public bool Click { get; set; }
        public string ClickUri { get; set; }
        public string NonClickUri { get; set; }
        public int Index { get; set; }
        public delegate void ClickEventHandler(LeftTab sender, bool click);
        public event ClickEventHandler ClickEventTick;

        public LeftTab()
        {
            InitializeComponent();

            this.Loaded += Control_Loaded;
        }

        [Bindable(true)]
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(LeftTab),
                new PropertyMetadata(null, new PropertyChangedCallback(TextChanged)));

        private static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (LeftTab)d;
            if (ctrl != null)
            {
                string newText = e.NewValue as string;
                ctrl.txtBlock.Text = newText;
            }
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            if (Click)
            {
                ClickIcon();
            }
            else
            {
                NonClickIcon();
            }
        }

        private void Click_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClickIcon();
            ClickEventTick?.Invoke(this, Click);
        }

        private void ClickIcon()
        {
            try
            {
                string packUri = !string.IsNullOrEmpty(ClickUri) ? ClickUri : "pack://application:,,,/Images/SampleRegister.png";
                image.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
                image.HorizontalAlignment = HorizontalAlignment.Left;
                mainGrid.Background = new SolidColorBrush(Color.FromRgb(6, 145, 157));
                txtBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
            catch { }
        }

        private void NonClickIcon()
        {
            try
            {
                string packUri = !string.IsNullOrEmpty(NonClickUri) ? NonClickUri : ClickUri;
                image.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
                image.HorizontalAlignment = HorizontalAlignment.Right;
                mainGrid.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                txtBlock.Foreground = new SolidColorBrush(Color.FromRgb(115, 87, 128));
            }
            catch { }
        }

        public void Reset()
        {
            Click = false;
            NonClickIcon();
        }
    }
}
