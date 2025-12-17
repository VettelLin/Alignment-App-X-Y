using General_PCR18.UControl;
using System.Windows;
using System.Windows.Controls;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for SettingPage.xaml
    /// </summary>
    public partial class SettingPage : Page
    {
        private readonly SettingPageSelect ps;

        private readonly PcrSetPage pcrSetPage = new PcrSetPage();
        private readonly UserManagePage userManagePage = new UserManagePage();

        public SettingPage()
        {
            InitializeComponent();

            ps = new SettingPageSelect(); //实例化PageSelect，初始选择页ps
            ps.LoadedEventTick += Ps_LoadedEventTick;
            FrameWork.Content = new Frame() { Content = ps };
        }

        private void Ps_LoadedEventTick(SettingPageSelect sender, bool loaded)
        {
            // 默认页            
            NavPage(pcrSetPage);
        }

        private void NavPage(BasePage page)
        {
            ps.NavigatePage(page);
        }

        private void Tab1_ClickEventTick(LeftTab sender, bool click)
        {
            foreach (FrameworkElement element in leftMenu.Children)
            {
                var tab = (element as DockPanel).Children[0] as LeftTab;
                if (tab.Index != sender.Index)
                {
                    tab.Reset();
                }
            }

            switch (sender.Index)
            {
                case 0:
                    NavPage(pcrSetPage);
                    break;
                case 1:
                    NavPage(userManagePage);
                    break;
            }
        }
    }
}
