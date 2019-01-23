using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionServerModules
{
    public static class EnumHelper
    {
        // get enum name (according to enum value)
        public static string GetEnumName<T>(this int value)
        {
            return Enum.GetName(typeof(T), value);
        }
        // get enum name collection
        public static string[] GetNamesArr<T>()
        {
            return Enum.GetNames(typeof(T));
        }

        // enum -> dictionary
        public static Dictionary<string, int> getEnumDic<T>()
        {

            Dictionary<string, int> resultList = new Dictionary<string, int>();
            Type type = typeof(T);
            var strList = GetNamesArr<T>().ToList();
            foreach (string key in strList)
            {
                string val = Enum.Format(type, Enum.Parse(type, key), "d");
                resultList.Add(key, int.Parse(val));
            }
            return resultList;
        }
        public static Dictionary<string, int> GetDic<TEnum>()
        {
            Dictionary<string, int> dic = new Dictionary<string, int>();
            Type t = typeof(TEnum);
            // method 1
            Dictionary<string, int> dic1 = new Dictionary<string, int>();
            var strList = GetNamesArr<TEnum>().ToList();
            foreach (string key in strList)
            {
                string val = Enum.Format(t, Enum.Parse(t, key), "d");
                dic1.Add(key, int.Parse(val));
            }
            // method 2
            Dictionary<string, int> dic2 = new Dictionary<string, int>();
            var arr = Enum.GetValues(t);
            foreach (var item in arr)
            {
                dic2.Add(item.ToString(), (int)item);
            }
            if (dic1 == dic2)
            {
                dic = dic1;
            }

            return dic;
        }

    }
}
