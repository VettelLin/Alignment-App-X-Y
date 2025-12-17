using General_PCR18.Common;
using General_PCR18.UControl;
using General_PCR18.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for SampleRegistrationPage.xaml
    /// </summary>
    public partial class SampleRegistrationPage : BasePage
    {
        #region 变量区域
        private readonly SampleUC[] sampleList = new SampleUC[18];
        private readonly HashSet<SampleUC> selectList = new HashSet<SampleUC>();

        private readonly SynchronizationContext context;

        #endregion

        public SampleRegistrationPage()
        {
            InitializeComponent();

            // 初始化样本
            SampleData sampleData = new SampleData()
            {
                Width = 120,
                Height = 150,
                SeparateHeight = 20,
                Margin = 20,
                Sample_ClickEventTick = Sample_ClickEventTick,
                Sample_StartClickEventHandler = Sample_StartClickEventHandler,
            };
            InitSample(sampleGrid, sampleList, sampleData);

            SampleEditActivate(false);

            context = SynchronizationContext.Current; // 获取当前 UI 线程的上下文

            // 订阅事件
            EventBus.OnSampleRegistrationMessageReceived += EventBus_OnMessageReceived;

            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("===>SampleRegistrationPage Loaded");

            RefreshSampleUC(sampleList);

            // 显示会话中的备份路径（若已选择）
            if (!string.IsNullOrWhiteSpace(GlobalData.BackupDataPath))
            {
                try { txtBackupPath.Text = GlobalData.BackupDataPath; } catch { }
            }
        }

        /// <summary>
        /// 处理全局事件
        /// </summary>
        /// <param name="obj"></param>
        private void EventBus_OnMessageReceived(NotificationMessage obj)
        {
            switch (obj.Code)
            {
                case MessageCode.PcrKeyStatus:
                    {
                        //LogHelper.Debug("SampleRegistration收到消息: " + obj.Code);

                        context.Post(_ => { RefreshSampleUC(sampleList, true); }, null);
                    }
                    break;
                case MessageCode.RefreshUI:
                    {
                        //LogHelper.Debug("SampleRegistration收到消息: " + obj.Code);

                        // 刷新按钮状态
                        { context.Post(_ => { RefreshSampleUC(sampleList, true); }, null); }
                    }
                    break;
            }
        }

        private void dpTestDate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        // 选择备份目录（不影响设置页，当前会话有效）
        private void BrowseBackup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var directoryPath = dialog.SelectedPath;
                GlobalData.BackupDataPath = directoryPath;
                try { txtBackupPath.Text = directoryPath; } catch { }
            }
        }

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

        /// <summary>
        /// 编辑框状态切换
        /// </summary>
        /// <param name="activate"></param>
        private void SampleEditActivate(bool activate)
        {
            if (activate)
            {
                txtSampleId.IsReadOnly = false;
                txtPatientId.IsReadOnly = false;
                cmbSampleType.IsEnabled = true;
                dpTestDate.IsEnabled = true;
            }
            else
            {
                txtSampleId.IsReadOnly = true;
                txtPatientId.IsReadOnly = true;
                cmbSampleType.IsEnabled = false;
                dpTestDate.IsEnabled = false;
            }
        }

        /// <summary>
        /// 点击样本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Sample_ClickEventTick(SampleUC sender, bool click)
        {
            SampleEditActivate(true);

            // 不按Ctr时是单选
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                selectList.Clear();

                // 回填值
                txtSampleId.Text = GlobalData.DS.HeatSampleID[sender.Index];
                txtPatientId.Text = GlobalData.DS.HeatPatientID[sender.Index];
                int typeId = GlobalData.DS.HeatSampleType[sender.Index];

                if (typeId > 0)
                {
                    foreach (ComboBoxItem item in cmbSampleType.Items)
                    {
                        if (int.Parse(item.Tag.ToString()) == typeId)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }
                else
                {
                    cmbSampleType.SelectedIndex = -1;
                }

                var dateStr = GlobalData.DS.HeatDateSample[sender.Index];
                if (!string.IsNullOrEmpty(dateStr))
                {
                    dpTestDate.SelectedDate = DateTime.Parse(dateStr);
                }
                else
                {
                    dpTestDate.SelectedDate = null;
                }

                selectList.Add(sender);
            }
            else
            {
                // 多选
                var sam = selectList.Where(s => s.Index == sender.Index).FirstOrDefault();
                if (sam != null)
                {
                    selectList.Remove(sam);
                }
                else
                {
                    selectList.Add(sender);
                }

                // 清空选项
                cmbSampleType.SelectedIndex = -1;
            }

            ChangeSelectBg(selectList, sampleList);
        }

        /// <summary>
        /// 开始按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Sample_StartClickEventHandler(SampleUC sender, bool click)
        {
            StartButtonClick(sender);
        }

        /// <summary>
        /// 点击X轴
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int lastCount = selectList.Count;
            SampleEditActivate(true);

            selectList.Clear();
            string text = (sender as Label).Content.ToString();
            AddSampleAxis(text, selectList, sampleList, lastCount);

            ChangeSelectBg(selectList, sampleList);
        }

        private void txtSampleId_LostFocus(object sender, RoutedEventArgs e)
        {
            // 检查输入状态
            CheckInputStatus(selectList);
        }

        /// <summary>
        /// 填写样本ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSampleId_TextChanged(object sender, TextChangedEventArgs e)
        {
            var ctr = sender as TextBox;
            string val = ctr.Text.Trim();
            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            if (selectList.Count > 0)
            {
                foreach (var t in selectList)
                {
                    GlobalData.DS.HeatSampleID[t.Index] = val;
                }
            }

			// 根据样本ID中的关键字自动选择样本类型（HPV/DNA/RNA）
			AutoSelectSampleTypeFromSampleId(val);
        }

		/// <summary>
		/// 从样本ID中解析关键字并自动选择样本类型。
		/// 优先级：HPV > DNA > RNA。
		/// </summary>
		/// <param name="sampleId"></param>
		private void AutoSelectSampleTypeFromSampleId(string sampleId)
		{
			if (string.IsNullOrWhiteSpace(sampleId))
			{
				return;
			}

			string upper = sampleId.ToUpperInvariant();

			int? targetTypeId = null; // 1=HPV, 2=RNA, 3=DNA（与 XAML Tag 保持一致）
			if (upper.Contains("HPV"))
			{
				targetTypeId = 1;
			}
			else if (upper.Contains("DNA"))
			{
				targetTypeId = 3;
			}
			else if (upper.Contains("RNA"))
			{
				targetTypeId = 2;
			}

			if (!targetTypeId.HasValue)
			{
				return;
			}

			// 若当前已是目标类型则不重复设置
			var currentItem = cmbSampleType.SelectedItem as ComboBoxItem;
			if (currentItem != null)
			{
				if (int.TryParse(currentItem.Tag.ToString(), out int currentTypeId) && currentTypeId == targetTypeId.Value)
				{
					return;
				}
			}

			foreach (ComboBoxItem item in cmbSampleType.Items)
			{
				if (int.Parse(item.Tag.ToString()) == targetTypeId.Value)
				{
					item.IsSelected = true; // 触发 SelectionChanged，自动写入到选中样本
					break;
				}
			}
		}

        private void txtPatientId_LostFocus(object sender, RoutedEventArgs e)
        {
            // 检查输入状态
            CheckInputStatus(selectList);
        }

        /// <summary>
        /// 填写患者ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtPatientId_TextChanged(object sender, TextChangedEventArgs e)
        {
            var ctr = sender as TextBox;
            string val = ctr.Text.Trim();
            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            if (selectList.Count > 0)
            {
                foreach (var t in selectList)
                {
                    GlobalData.DS.HeatPatientID[t.Index] = val;

                    sampleList[t.Index].PatientId = val;
                }
            }
        }

        /// <summary>
        /// 选择样本类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbSampleType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                return;
            }

            string val = selectedItem.Tag.ToString();
            int typeId = int.Parse(val);

            if (selectList.Count > 0)
            {
                foreach (var s in selectList)
                {
                    GlobalData.DS.HeatSampleType[s.Index] = typeId;
                    sampleList[s.Index].SampleTypeText = VarDef.SampleType[typeId][0];

                    // 设置默认值
                    GlobalData.DS.HeatH1Temp[s.Index] = (int)(double.Parse(VarDef.DefaultValues[typeId][0]) * 10);
                    GlobalData.DS.HeatH1Time[s.Index] = int.Parse(VarDef.DefaultValues[typeId][1]);
                    GlobalData.DS.HeatH3Temp[s.Index] = (int)(double.Parse(VarDef.DefaultValues[typeId][2]) * 10);
                    GlobalData.DS.HeatH3Time[s.Index] = int.Parse(VarDef.DefaultValues[typeId][1]);

                    // 边框颜色
                    s.BorderColor = Tools.HexToBrush(VarDef.SampleType[typeId][1]);
                    // 选中背景颜色
                    s.BackgroundColor = Tools.HexToBrush(VarDef.SampleType[typeId][2]);
                }

                // 检查输入状态
                CheckInputStatus(selectList);
            }
        }

        /// <summary>
        /// 选择日期
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dpTestDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var dp = sender as DatePicker;
            if (dp.SelectedDate.HasValue)
            {
                var val = dp.SelectedDate.Value.ToString("yyyy-MM-dd");
                if (selectList.Count > 0)
                {
                    foreach (var t in selectList)
                    {
                        GlobalData.DS.HeatDateSample[t.Index] = val;
                    }
                }
            }

            // 检查输入状态
            CheckInputStatus(selectList);
        }
    }
}
