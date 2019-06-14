using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TransactionServer.Jobs.ACAPServer
{
    public static class RFC2617Authorization
    {
        public static string GenerateWwwAuthenticate(RFC2617AuthHeader authHeader)
        {
            return string.Format("{0} {1},{2},{3},{4}",
                authHeader.m_type.GetDescription(),
                authHeader.SerializeData(RFC2617AuthorizationData.AUTHORIZATION_REALM),
                authHeader.SerializeData(RFC2617AuthorizationData.AUTHORIZATION_NONCE),
                authHeader.SerializeData(RFC2617AuthorizationData.AUTHORIZATION_ALGORITHM),
                authHeader.SerializeData(RFC2617AuthorizationData.AUTHORIZATION_QOP));
        }

        public static RFC2617AuthHeader GenerateAuthHeader(string header)
        {
            try
            {
                string type = header.Substring(0, header.IndexOf(' '));
                RFC2617AuthorizationType authType = type.GetValueByDescription<RFC2617AuthorizationType>();
                if (RFC2617AuthorizationType.AUTHORIZATION_TYPE_DIGEST != authType)
                {
                    return null;
                }

                RFC2617AuthHeader authHeader = new RFC2617AuthHeader();
                authHeader.SetAuthorizationType(authType);
                string authInfo = header.Replace(type, string.Empty).Trim();
                string[] items = authInfo.Split(',');
                foreach (string item in items)
                {
                    bool isDeserialized = authHeader.DeserializeData(item);
                    if (!isDeserialized)
                    {
                        continue;
                    }
                }
                return authHeader;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static bool CheckAuthHeader(RFC2617AuthHeader authHeader, string method, string username, string password)
        {
            if (!authHeader.m_username.Equals(username))
            {
                return false;
            }

            string response = CaculateResponse(authHeader, method, username, password);
            if (!authHeader.m_response.Equals(response))
            {
                return false;
            }

            return true;
        }

        /*
         * HA1 = md5(username:realm:password)
         * HD = nonce:noncecount:cnonce:qop
         * HA2 = md5(method:uri)
         * response = md5(HA1:HD:HA2)
         */
        private static string CaculateResponse(RFC2617AuthHeader authHeader, string method, string username, string password)
        {
            StringBuilder builder = new StringBuilder();
            string HA1 = null, HD = null, HA2 = null;

            builder.Append(username).Append(":").Append(authHeader.m_realm).Append(":").Append(password);
            HA1 = MD5Util.MD5Encrypt(builder.ToString());
            builder.Clear();

            HD = builder.Append(authHeader.m_nonce).Append(":").Append(authHeader.m_nc).Append(":").Append(authHeader.m_cnonce).Append(":").Append(authHeader.m_qop).ToString();
            builder.Clear();

            builder.Append(method).Append(":").Append(authHeader.m_uri);
            HA2 = MD5Util.MD5Encrypt(builder.ToString());
            builder.Clear();

            builder.Append(HA1).Append(":").Append(HD).Append(":").Append(HA2);
            string response = MD5Util.MD5Encrypt(builder.ToString());
            return response;
        }
    }

    public enum RFC2617AuthorizationType
    {
        [Description("Basic")]
        AUTHORIZATION_TYPE_BASIC = 0,
        [Description("Digest")]
        AUTHORIZATION_TYPE_DIGEST,
    }

    public enum RFC2617AuthorizationData
    {
        [Description("username")]
        AUTHORIZATION_USERNAME = 0,
        [Description("realm")]
        AUTHORIZATION_REALM,
        [Description("nonce")]
        AUTHORIZATION_NONCE,
        [Description("uri")]
        AUTHORIZATION_URI,
        [Description("cnonce")]
        AUTHORIZATION_CNONCE,
        [Description("nc")]
        AUTHORIZATION_NC,
        [Description("qop")]
        AUTHORIZATION_QOP,
        [Description("response")]
        AUTHORIZATION_RESPONSE,
        [Description("algorithm")]
        AUTHORIZATION_ALGORITHM,
        [Description("opaque")]
        AUTHORIZATION_OPAQUE,
    }

    public class RFC2617AuthHeader
    {
        public RFC2617AuthorizationType m_type { get; private set; } = RFC2617AuthorizationType.AUTHORIZATION_TYPE_DIGEST;
        public string m_realm { get; private set; } = string.Empty;
        public string m_nonce { get; private set; } = string.Empty;
        public string m_algorithm { get; private set; } = string.Empty;
        public string m_qop { get; private set; } = string.Empty;
        public string m_username { get; private set; } = string.Empty;
        public string m_uri { get; private set; } = string.Empty;
        public string m_cnonce { get; private set; } = string.Empty;
        public string m_nc { get; private set; } = string.Empty;
        public string m_response { get; private set; } = string.Empty;

        public RFC2617AuthHeader()
        {
            //
        }

        public RFC2617AuthHeader(RFC2617AuthorizationType type, string realm, string qop, string algorithm)
        {
            this.m_type = type;
            this.m_realm = realm;
            this.m_qop = qop;
            this.m_algorithm = algorithm;
        }

        public string EncodeToNonce(string str)
        {
            Encoding encoding = ASCIIEncoding.ASCII;
            byte[] strByte = encoding.GetBytes(str);
            return Convert.ToBase64String(strByte);
        }

        public string DecodeFromNonce(string str)
        {
            byte[] strByte = Convert.FromBase64String(str);
            Encoding encoding = ASCIIEncoding.ASCII;
            return encoding.GetString(strByte);
        }

        public void SetAuthorizationType(RFC2617AuthorizationType type)
        {
            this.m_type = type;
        }

        public bool Get(RFC2617AuthorizationData type, out string value)
        {
            switch (type)
            {
                case RFC2617AuthorizationData.AUTHORIZATION_USERNAME:
                    value = m_username;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_REALM:
                    value = m_realm;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_NONCE:
                    value = m_nonce;
                    //value = DecodeFromNonce(m_nonce);
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_URI:
                    value = m_uri;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_CNONCE:
                    value = m_cnonce;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_NC:
                    value = m_nc;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_QOP:
                    value = m_qop;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_RESPONSE:
                    value = m_response;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_ALGORITHM:
                    value = m_algorithm;
                    return true;
                default:
                    value = string.Empty;
                    return false;
            }
        }

        public bool Set(RFC2617AuthorizationData type, string value)
        {
            switch (type)
            {
                case RFC2617AuthorizationData.AUTHORIZATION_USERNAME:
                    this.m_username = value;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_REALM:
                    this.m_realm = value;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_NONCE:
                    this.m_nonce = value;
                    //this.m_nonce = EncodeToNonce(value);
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_URI:
                    this.m_uri = value;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_CNONCE:
                    this.m_cnonce = value;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_NC:
                    this.m_nc = value;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_QOP:
                    this.m_qop = value;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_RESPONSE:
                    this.m_response = value;
                    return true;
                case RFC2617AuthorizationData.AUTHORIZATION_ALGORITHM:
                    this.m_algorithm = value;
                    return true;
                default:
                    return false;
            }
        }

        public string SerializeData(RFC2617AuthorizationData type)
        {
            string key = type.GetDescription();
            string value;
            return Get(type, out value) ? SerializeData(key, value) : string.Empty;
        }

        private string SerializeData(string key, string value)
        {
            return key == string.Empty ? string.Empty : string.Format("{0}=\"{1}\"", key, value);
        }

        public bool DeserializeData(string data)
        {
            if (2 != data.Split('=').Length)
            {
                return false;
            }

            try
            {
                string key = data.Split('=')[0].Trim();
                string value = GetInnerValue(data.Split('=')[1].Trim());
                RFC2617AuthorizationData type = key.GetValueByDescription<RFC2617AuthorizationData>();
                return Set(type, value);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string GetInnerValue(string value)
        {
            Regex regex = new Regex("\"[^\"]*\"");
            Capture capture = regex.Match(value);
            return capture.Value == string.Empty ? value : capture.Value.Replace("\"", "");
        }
    }

    public class MD5Util
    {
        public static string MD5Encrypt(string input)
        {
            return MD5Encrypt(input, new UTF8Encoding());
        }

        public static string MD5Encrypt(string input, int length)
        {
            string res = MD5Encrypt(input, new UTF8Encoding());   // 16/32
            if (16 == length)
            {
                res = res.Substring(8, 16);
            }
            return res;
        }

        public static string MD5Encrypt(string input, Encoding encode)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(encode.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }

    public static class EnumHelper
    {
        public static string GetDescription<T>(this T value) where T : struct
        {
            string result = value.ToString();
            Type type = typeof(T);
            FieldInfo fieldInfo = type.GetField(value.ToString());
            var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
            if ((null != attributes) && (null != attributes.FirstOrDefault()))
            {
                result = (attributes.First() as DescriptionAttribute).Description;
            }

            return result;
        }

        public static T GetValueByDescription<T>(this string description) where T : struct
        {
            Type type = typeof(T);
            foreach (var fieldInfo in type.GetFields())
            {
                if (description == fieldInfo.Name)
                {
                    return (T)fieldInfo.GetValue(null);
                }

                var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if ((null != attributes) && (null != attributes.FirstOrDefault()))
                {
                    if (description == attributes.First().Description)
                    {
                        return (T)fieldInfo.GetValue(null);
                    }
                }
            }

            throw new ArgumentException(string.Format("{0} not found enum type.", description), "Description");
        }

        public static T GetValue<T>(this string value) where T : struct
        {
            T result;
            if (Enum.TryParse(value, true, out result))
            {
                return result;
            }

            throw new ArgumentException(string.Format("{0} not found enum type.", value), "Value");
        }
    }
}
