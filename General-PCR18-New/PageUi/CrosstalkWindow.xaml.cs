using System;
using System.Windows;

namespace General_PCR18.PageUi
{
    public partial class CrosstalkWindow : Window
    {
        public CrosstalkWindow()
        {
            InitializeComponent();
            try
            {
                var m = Common.GlobalData.CrosstalkMatrix;
                Set(txt_00, m[0,0]); Set(txt_01, m[0,1]); Set(txt_02, m[0,2]); Set(txt_03, m[0,3]); Set(txt_04, m[0,4]);
                Set(txt_10, m[1,0]); Set(txt_11, m[1,1]); Set(txt_12, m[1,2]); Set(txt_13, m[1,3]); Set(txt_14, m[1,4]);
                Set(txt_20, m[2,0]); Set(txt_21, m[2,1]); Set(txt_22, m[2,2]); Set(txt_23, m[2,3]); Set(txt_24, m[2,4]);
                Set(txt_30, m[3,0]); Set(txt_31, m[3,1]); Set(txt_32, m[3,2]); Set(txt_33, m[3,3]); Set(txt_34, m[3,4]);
                Set(txt_40, m[4,0]); Set(txt_41, m[4,1]); Set(txt_42, m[4,2]); Set(txt_43, m[4,3]); Set(txt_44, m[4,4]);
            }
            catch { }
        }

        private void Set(System.Windows.Controls.TextBox t, double v)
        {
            t.Text = v.ToString("0.000");
        }

        private double Get(System.Windows.Controls.TextBox t)
        {
            return double.TryParse(t.Text, out var x) ? x : 0.0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var m = Common.GlobalData.CrosstalkMatrix;
                m[0,0]=Get(txt_00); m[0,1]=Get(txt_01); m[0,2]=Get(txt_02); m[0,3]=Get(txt_03); m[0,4]=Get(txt_04);
                m[1,0]=Get(txt_10); m[1,1]=Get(txt_11); m[1,2]=Get(txt_12); m[1,3]=Get(txt_13); m[1,4]=Get(txt_14);
                m[2,0]=Get(txt_20); m[2,1]=Get(txt_21); m[2,2]=Get(txt_22); m[2,3]=Get(txt_23); m[2,4]=Get(txt_24);
                m[3,0]=Get(txt_30); m[3,1]=Get(txt_31); m[3,2]=Get(txt_32); m[3,3]=Get(txt_33); m[3,4]=Get(txt_34);
                m[4,0]=Get(txt_40); m[4,1]=Get(txt_41); m[4,2]=Get(txt_42); m[4,3]=Get(txt_43); m[4,4]=Get(txt_44);
                Common.GlobalData.CrosstalkMatrix = m;
                // 持久化写入 Config.xml
                try { General_PCR18.Util.ConfigXMLHelper.WriteCrosstalkMatrix(m); } catch { }
                DialogResult = true;
            }
            catch { DialogResult = false; }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


