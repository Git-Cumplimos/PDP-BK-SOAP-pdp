using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServiceBancos.templates_lib
{
    public class Utils
    {
        public string JsonSerializerCustom(Dictionary<string, dynamic> data)
        {
            List<string> data_list = new List<string>();
            var p = data;
            foreach (KeyValuePair<string, dynamic> data_ind in data)
            {
                string value = $"{data_ind.Value}";
                value = value.Replace('\"', '`');
                value = value.Replace("\r", "\\r");
                value = value.Replace("\n", "\\n");
                data_list.Add($"\"{data_ind.Key}\" : \"{value}\" ");
            }

            string data_string= string.Join(", ", data_list);
            data_string = "{ " + data_string + " }";
            return data_string;
        }
    }
}