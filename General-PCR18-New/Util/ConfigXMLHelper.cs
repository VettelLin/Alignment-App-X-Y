using System;
using System.IO;
using System.Xml;
using static General_PCR18.Util.ConfigParam;

namespace General_PCR18.Util
{
    public static class ConfigXMLHelper
    {
        private readonly static string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Config.xml");

        public static void ReadXml()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                var node = doc.SelectSingleNode("appSettings");
                ConfigParam.LogLevel = (LogLevelEnum)Enum.Parse(typeof(LogLevelEnum), node.SelectSingleNode("LogLevel").InnerText);
                ConfigParam.LogFilePath = node.SelectSingleNode("LogFilePath").InnerText.Trim();
                ConfigParam.LogFileExistDay = int.Parse(node.SelectSingleNode("LogFileExistDay").InnerText);
                ConfigParam.DevicePort = node.SelectSingleNode("DevicePort").InnerText.Trim();

                // 可选：CrosstalkMatrix 持久化（5行, 每行5个, 逗号分隔）
                try
                {
                    var ctNode = node.SelectSingleNode("Crosstalk");
                    if (ctNode != null)
                    {
                        string text = ctNode.InnerText.Trim();
                    string[] rows = text.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < 5 && i < rows.Length; i++)
                        {
                            string[] cols = rows[i].Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int j = 0; j < 5 && j < cols.Length; j++)
                            {
                                if (double.TryParse(cols[j], out var v))
                                {
                                    General_PCR18.Common.GlobalData.CrosstalkMatrix[i, j] = v;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                LogHelper.log.Error(string.Format("XML文件读取失败。{0}", ex));
            }
        }

        public static void WriteXml(string key, string val)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                var node = doc.SelectSingleNode("appSettings");
                node.SelectSingleNode(key).InnerText = val;

                doc.Save(file);
            }
            catch (Exception ex)
            {
                LogHelper.log.Error(string.Format("写XML失败。{0}", ex));
            }
        }

        public static void WriteCrosstalkMatrix(double[,] m)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                var app = doc.SelectSingleNode("appSettings");
                var node = app.SelectSingleNode("Crosstalk");
                if (node == null)
                {
                    node = doc.CreateElement("Crosstalk");
                    app.AppendChild(node);
                }
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < 5; i++)
                {
                    if (i > 0) sb.Append('\n');
                    for (int j = 0; j < 5; j++)
                    {
                        if (j > 0) sb.Append(',');
                        sb.Append(m[i, j].ToString("0.###"));
                    }
                }
                node.InnerText = sb.ToString();
                doc.Save(file);
            }
            catch (Exception ex)
            {
                LogHelper.log.Error(string.Format("写Crosstalk失败。{0}", ex));
            }
        }
    }
}
