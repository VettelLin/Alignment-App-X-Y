using System;
using System.Collections.Generic;
using System.Management;

namespace General_PCR18.Util
{
    public class SystemInfoUtil
    {
        public static List<string> Cpu()
        {
            List<string> results = new List<string>();
            ManagementClass managementClass = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = managementClass.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                results.Add(mo.Properties["ProcessorId"].Value.ToString().Trim());
            }
            return results;
        }

        public static List<string> Disk()
        {
            List<string> results = new List<string>();
            ManagementClass managementClass = new ManagementClass("Win32_PhysicalMedia");
            ManagementObjectCollection moc = managementClass.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                results.Add(mo.Properties["SerialNumber"].Value.ToString().Trim());
            }
            return results;
        }

        public static string BIOS()
        {
            string results = "";
            ManagementClass managementClass = new ManagementClass("Win32_BIOS");
            ManagementObjectCollection moc = managementClass.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                results = mo.Properties["SerialNumber"].Value.ToString().Trim();
                break;
            }
            return results;
        }

        public static List<string> Network()
        {
            List<string> results = new List<string>();
            ManagementClass managementClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = managementClass.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"] == true)
                {
                    results.Add(mo.Properties["MacAddress"].Value.ToString().Trim());
                }
            }
            return results;
        }

        public static string GetUUID()
        {
            try
            {
                ManagementClass mc = new ManagementClass("Win32_DiskDrive");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    string mType = mo.Properties["MediaType"].Value?.ToString();
                    string strID = mo.Properties["SerialNumber"].Value.ToString();
                    // Console.WriteLine(JsonUtil.ToJson(mo.Properties));
                    Console.WriteLine("===>ID:{0}, TYPE:{1}", strID, mType);

                    if (!string.IsNullOrEmpty(mType) && mType.Contains("hard"))
                    {
                        return strID.Replace(" ", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return "";
        }
    }
}
