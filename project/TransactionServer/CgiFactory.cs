using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SeasideResearch.LibCurlNet;
using System.Diagnostics;

namespace TransactionServer
{
    public class CgiFactory
    {
        public enum CURL_METHOD
        {
            CURL_METHOD_GET = 1,
            CURL_METHOD_POST = 2
        }

        public struct CURL_HTTP_ARGS
        {
            public CURL_METHOD Method;
            public string Url;
            // auth
            public long AuthType;
            public string UserName;
            public string Password;
            // post
            public string PostData;
            public string PostFile; // file name
            // result in cache
            public int DataLength;
            public string Data;
        }

        public const CURL_METHOD DEFAULT_METHOD = CURL_METHOD.CURL_METHOD_GET;
        public const long AUTH_TYPE = (long)(CURLhttpAuth.CURLAUTH_DIGEST | CURLhttpAuth.CURLAUTH_BASIC);
        public const string USERNAME = "root";
        public const string PASSWORD = "pass";
        public const string POST_DATA = "";
        public const int TIMEOUT = 3600;

        public CURL_HTTP_ARGS m_args;

        public void CURL_Init()
        {
            m_args = new CURL_HTTP_ARGS();
            m_args.Method = DEFAULT_METHOD;
            m_args.Url = string.Empty;
            m_args.AuthType = AUTH_TYPE;
            m_args.UserName = USERNAME;
            m_args.Password = PASSWORD;
            m_args.PostData = POST_DATA;
            m_args.PostFile = string.Empty;
            m_args.DataLength = -1;
            m_args.Data = string.Empty;
        }

        public void CURL_SetMethod(CURL_METHOD method)
        {
            m_args.Method = method;
        }
        public CURL_METHOD CURL_GetMethod()
        {
            return m_args.Method;
        }

        public void CURL_SetUrl(string url)
        {
            m_args.Url = url;
        }
        public string CURL_GetUrl()
        {
            return m_args.Url;
        }

        public void CURL_SetAuthType(long type)
        {
            m_args.AuthType = type;
        }
        public long CURL_GetAuthType()
        {
            return m_args.AuthType;
        }

        public void CURL_SetUserName(string name)
        {
            m_args.UserName = name;
        }
        public string CURL_GetUserName()
        {
            return m_args.UserName;
        }

        public void CURL_SetPassword(string pwd)
        {
            m_args.Password = pwd;
        }
        public string CURL_GetPassword()
        {
            return m_args.Password;
        }

        public void CURL_SetPostData(string data)
        {
            m_args.PostData = data;
        }
        public string CURL_GetPostData()
        {
            return m_args.PostData;
        }

        public void CURL_SetPostFile(string file)
        {
            m_args.PostFile = file;
        }
        public string CURL_GetPostFile()
        {
            return m_args.PostFile;
        }

        public int CURL_GetResponseDataLength()
        {
            return m_args.DataLength;
        }

        public string CURL_GetResponseData()
        {
            return m_args.Data;
        }

        public bool CURL_HTTP_Post(out string status, out string result)
        {
            bool ret = false;
            status = string.Empty;
            result = string.Empty;

            CURLcode code;
            code = Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_ALL);
            if (CURLcode.CURLE_OK != code)
            {
                status = code.ToString();
                Trace.WriteLine("curl init failed : {0}", status);
                return false;
            }

            Easy easy = new Easy();
            Easy.WriteFunction wf = new Easy.WriteFunction(OnWriteData);
            easy.SetOpt(CURLoption.CURLOPT_HEADER, 0);  // no need to pass http-header to callback function
            easy.SetOpt(CURLoption.CURLOPT_URL, m_args.Url);
            easy.SetOpt(CURLoption.CURLOPT_TIMEOUT, TIMEOUT);
            easy.SetOpt(CURLoption.CURLOPT_HTTPAUTH, m_args.AuthType);
            easy.SetOpt(CURLoption.CURLOPT_USERPWD, m_args.UserName + ":" + m_args.Password);   // [user name]:[password]
            easy.SetOpt(CURLoption.CURLOPT_WRITEFUNCTION, wf);
            // ordinary POST
            easy.SetOpt(CURLoption.CURLOPT_POST, true); // set POST method
            easy.SetOpt(CURLoption.CURLOPT_POSTFIELDS, m_args.PostData);
            easy.SetOpt(CURLoption.CURLOPT_POSTFIELDSIZE, m_args.PostData.Length);

            code = easy.Perform();
            if (CURLcode.CURLE_OK != code)
            {
                status = easy.StrError(code);
                Trace.WriteLine("curl perform failed : {0}", status);
                easy.Cleanup();
                Curl.GlobalCleanup();
                return false;
            }
            ret = true;
            status = "200 OK";

            if (null != m_args.Data)    // 0 != args.DataLength
            {
                result = m_args.Data;
                // reset for reusing
                m_args.Data = string.Empty;
                m_args.DataLength = 0;
            }

            easy.Cleanup();
            Curl.GlobalCleanup();

            return ret;
        }

        public bool CURL_HTTP_Get(out string status, out string result)
        {
            bool ret = false;
            status = string.Empty;
            result = string.Empty;

            CURLcode code;
            code = Curl.GlobalInit((int)CURLinitFlag.CURL_GLOBAL_ALL);
            if (CURLcode.CURLE_OK != code)
            {
                status = code.ToString();
                Trace.WriteLine("curl init failed : {0}", status);
                return false;
            }

            Easy easy = new Easy();
            Easy.WriteFunction wf = new Easy.WriteFunction(OnWriteData);
            easy.SetOpt(CURLoption.CURLOPT_HEADER, 0);  // no need to pass http-header to callback function
            easy.SetOpt(CURLoption.CURLOPT_URL, m_args.Url);
            easy.SetOpt(CURLoption.CURLOPT_TIMEOUT, TIMEOUT);
            easy.SetOpt(CURLoption.CURLOPT_HTTPAUTH, m_args.AuthType);
            easy.SetOpt(CURLoption.CURLOPT_USERPWD, m_args.UserName + ":" + m_args.Password);   // [user name]:[password]
            easy.SetOpt(CURLoption.CURLOPT_WRITEFUNCTION, wf);

            code = easy.Perform();
            if (CURLcode.CURLE_OK != code)
            {
                status = easy.StrError(code);
                Trace.WriteLine("curl perform failed : {0}", status);
                easy.Cleanup();
                Curl.GlobalCleanup();
                return false;
            }
            ret = true;
            status = "200 OK";

            if (null != m_args.Data)    // 0 != args.DataLength
            {
                result = m_args.Data;
                // reset for reusing
                m_args.Data = string.Empty;
                m_args.DataLength = 0;
            }

            easy.Cleanup();
            Curl.GlobalCleanup();

            return ret;
        }

        public Int32 OnWriteData(Byte[] buf, Int32 size, Int32 count, Object stream)
        {
            int len = size * count;

            m_args.Data += System.Text.Encoding.UTF8.GetString(buf);
            m_args.DataLength += len;

            return len;
        }
    }
}
