using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horde_Editor.Common
{
    static class JSON
    {
        public static Dictionary<string, string> ParseJson(string res)
        {
            var lines = res.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var ht = new Dictionary<string, string>(20);
            var st = new Stack<string>(20);

            for (int i = 0; i < lines.Length; ++i)
            {
                var line = lines[i];
                var pair = line.Split(":".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);

                if (pair.Length == 2)
                {
                    var key = ClearString(pair[0]);
                    var val = ClearString(pair[1]);

                    if (val == "{")
                    {
                        st.Push(key);
                    }
                    else
                    {
                        if (st.Count > 0)
                        {
                            key = string.Join("_", st) + "_" + key;
                        }

                        if (ht.ContainsKey(key))
                        {
                            ht[key] += "&" + val;
                        }
                        else
                        {
                            ht.Add(key, val);
                        }
                    }
                }
                else if (line.IndexOf('}') != -1 && st.Count > 0)
                {
                    st.Pop();
                }
            }

            return ht;
        }

        static string ClearString(string str)
        {
            str = str.Trim();

            var ind0 = str.IndexOf("\"");
            var ind1 = str.LastIndexOf("\"");

            if (ind0 != -1 && ind1 != -1)
            {
                str = str.Substring(ind0 + 1, ind1 - ind0 - 1);
            }
            else if (str[str.Length - 1] == ',')
            {
                str = str.Substring(0, str.Length - 1);
            }

            //str = System.Web.HttpUtility.UrlDecode(str);

            return str;
        }

        public static string WriteJson(Dictionary<string, string> dict)
        {
            // Пока что данные только одного уровня.
            string out_str = "{\n";
            int level = 1;
            string tab = new string(' ', level * 4);

            foreach (string s in dict.Keys)
            {
                out_str += tab + "\"" + s + "\": \"" + dict[s] + "\",\n";
            }

            out_str = out_str.Remove(out_str.Length - 2);
            out_str += "\n}";
            return out_str;
        }
    }

}
