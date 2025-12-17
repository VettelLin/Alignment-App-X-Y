using General_PCR18.Common;
using General_PCR18.DB;
using General_PCR18.UControl;
using General_PCR18.Util;
using System;
using System.Windows;
using System.Windows.Input;

namespace General_PCR18
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();

            this.Loaded += Windows_Loaded;
            this.Closing += Windows_Closing;
            this.Closed += Windows_Closed;
        }

        private void Windows_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            if (System.Windows.Forms.Screen.AllScreens.Length > 1)
            {
                this.Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
                this.Top = 0;
            }
#endif

            this.WindowState = WindowState.Maximized;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
        }

        private void Windows_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void Windows_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginAction();
        }

        private void LoginAction()
        {
            if (!CheckUser())
            {
                return;
            }

            if (GlobalData.MainWin == null)
            {
                GlobalData.MainWin = new MainWindow();
            }
            GlobalData.MainWin.Show();
            this.Hide();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private bool CheckUser()
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MyMessageBox.Show(Properties.Resources.username_password_empty,
                    MyMessageBox.CustomMessageBoxButton.OK,
                MyMessageBox.CustomMessageBoxIcon.Warning);
                return false;
            }

            UserDAL userDAL = new UserDAL();
            User user = userDAL.FindByUsername(username);
            if (user == null)
            {
                MyMessageBox.Show(Properties.Resources.username_password_error,
                    MyMessageBox.CustomMessageBoxButton.OK,
                MyMessageBox.CustomMessageBoxIcon.Warning);
                return false;
            }

            if (!CryptUtil.CheckHash(password, user.Password))
            {
                MyMessageBox.Show(Properties.Resources.username_password_error,
                    MyMessageBox.CustomMessageBoxButton.OK,
                MyMessageBox.CustomMessageBoxIcon.Warning);
                return false;
            }

            GlobalData.CurrentUser = user;

            return true;
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginAction();
            }
        }
    }
}
