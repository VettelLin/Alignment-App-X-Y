using General_PCR18.Common;
using System;
using System.IO;

namespace General_PCR18.Util
{
    public class CacheFileUtil
    {
        private readonly static string cacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "GeneralPCR18Cache.json");

        public static ConfigCache Read()
        {
            try
            {
                string json = File.ReadAllText(cacheFile);
                ConfigCache config = JsonUtil.FromJson<ConfigCache>(json);
                if (config == null || string.IsNullOrWhiteSpace(config.DataPath))
                {
                    return EnsureDefaultAndReturn(config ?? new ConfigCache());
                }
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine("读取配置文件出错：" + ex.Message);
                return EnsureDefaultAndReturn(new ConfigCache());
            }
        }

        public static void Save(ConfigCache config)
        {
            try
            {
                string json = JsonUtil.ToJson(config);
                string dir = Path.GetDirectoryName(cacheFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(cacheFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("保存配置文件出错：" + ex.Message);
            }
        }

        private static ConfigCache EnsureDefaultAndReturn(ConfigCache config)
        {
            try
            {
                string defaultPath = @"C:\\";
                if (!Directory.Exists(defaultPath))
                {
                    Directory.CreateDirectory(defaultPath);
                }
                config.DataPath = defaultPath;
                Save(config);
            }
            catch
            {
                // ignore
            }
            return config;
        }
    }
}
