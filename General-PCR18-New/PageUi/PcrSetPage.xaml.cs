using General_PCR18.Common;
using General_PCR18.Util;
using System;
using System.Windows.Controls;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for PcrSetPage.xaml
    /// </summary>
    public partial class PcrSetPage : BasePage
    {
        public PcrSetPage()
        {
            InitializeComponent();

            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                ConfigCache configCache = CacheFileUtil.Read();
                if (configCache == null)
                {
                    return;
                }

                // 强制将数据路径设置为 C:\（作为备份目录），不随之前的配置变化
                if (true)
                {
                    string defaultPath = @"C:\";
                    try
                    {
                        if (!System.IO.Directory.Exists(defaultPath))
                        {
                            System.IO.Directory.CreateDirectory(defaultPath);
                        }
                        configCache.DataPath = defaultPath;
                        CacheFileUtil.Save(configCache);
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(configCache.Lang))
                {
                    foreach (ComboBoxItem item in cmbLanguage.Items)
                    {
                        if (item.Tag?.ToString() == configCache.Lang)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }

                txtBoxDetectionTime.Text = configCache.DetectionTime?.Trim();
                txtBoxDataPath.Text = configCache.DataPath?.Trim();
            }
            catch (Exception ex)
            {
                LogHelper.Error((object)"加载配置出错", ex);
            }
        }

        private void cmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count <= 0)
            {
                return;
            }

            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            string tag = selectedItem.Tag.ToString();
            ConfigCache configCache = CacheFileUtil.Read();

            if (tag.Equals("en-US"))
            {
                Lang.RService.Current.ChangedCulture("en-US");
                configCache.Lang = "en-US";
            }
            else
            {
                Lang.RService.Current.ChangedCulture("");
                configCache.Lang = "";
            }

            CacheFileUtil.Save(configCache);

            Console.WriteLine("切换语言: " + tag);
        }

        private void Browse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer; // 设置初始目录
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string directoryPath = folderBrowserDialog.SelectedPath; // 获取选中的文件夹的完整路径
                txtBoxDataPath.Text = directoryPath;

                ConfigCache configCache = CacheFileUtil.Read();
                configCache.DataPath = directoryPath;
                CacheFileUtil.Save(configCache);
            }
        }

        private void txtBoxDetectionTime_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            string val = (sender as System.Windows.Controls.TextBox).Text?.Trim();
            if (!string.IsNullOrEmpty(val))
            {
                ConfigCache configCache = CacheFileUtil.Read();
                configCache.DetectionTime = val;
                CacheFileUtil.Save(configCache);
            }
        }
    }
}
