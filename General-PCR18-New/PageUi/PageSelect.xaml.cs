using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for PageSelect.xaml
    /// </summary>
    public partial class PageSelect : Page
    {
        private NavigationService navigationService;
        public delegate void LoadedEventHandler(PageSelect sender, bool loaded);
        public event LoadedEventHandler LoadedEventTick;

        public PageSelect()
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
