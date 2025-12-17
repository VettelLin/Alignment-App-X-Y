using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for SettingPageSelect.xaml
    /// </summary>
    public partial class SettingPageSelect : Page
    {
        private NavigationService navigationService;
        public delegate void LoadedEventHandler(SettingPageSelect sender, bool loaded);
        public event LoadedEventHandler LoadedEventTick;

        public SettingPageSelect()
        {
            InitializeComponent();

            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            navigationService = NavigationService.GetNavigationService(this);
            LoadedEventTick?.Invoke(this, true);
        }

        public void NavigatePage(BasePage page)
        {
            if (navigationService != null)
            {
                navigationService.Navigate(page);
            }
        }
    }
}
