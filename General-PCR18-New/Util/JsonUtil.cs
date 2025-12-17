using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace General_PCR18.Util
{
    public class JsonUtil
    {
        /// <summary>
        /// 转换对象到JSON串
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string ToJson(object o)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                DateFormatString = "yyyy-MM-dd HH:mm:ss"
            };
            var json = JsonConvert.SerializeObject(o, settings);
            return json;
        }

        /// <summary>
        /// JSON串转换到对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static object FromJson(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type);
        }

        public static string ReadJsonKey(string key, string jsonFile)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonFile))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        JObject o = (JObject)JToken.ReadFrom(reader);
                        var value = o[key].ToString();
                        return value;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static Dictionary<string, object> ObjectToDictionary(object obj)
        {
            var dictionary = JObject.FromObject(obj).ToObject<Dictionary<string, object>>();
            return dictionary;
        }
    }
}
