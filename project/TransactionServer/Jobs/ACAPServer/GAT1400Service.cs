using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.IO;
using System.Net;
using TransactionServer.Base;


namespace TransactionServer.Jobs.ACAPServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    public class GAT1400Service : IServiceCom
    {
        private Dictionary<string, APEInfo> m_mapAPE = new Dictionary<string, APEInfo>();
        private List<APE> deviceList = new List<APE>();
        private Dictionary<string, string> m_mapRegistration = new Dictionary<string, string>();
        private int m_nonce = 0;
        private string m_workDirectory = string.Empty;
        private string m_acapFile = string.Empty;
        public event EventHandler<EventArgs> APEInfoUpdateEvent;
        private bool m_bTraceLogEnabled = true;
        private bool m_bPrintLogEnabled = false;

        private const string AUTH_TYPE_STRING = "Digest";
        private const string FILE_FORMAT = ".csv";
        private const string APEINFO_FILENAME = "APEInfo";
        private const string REGINTO_FILENAME = "RegInfo";
        private const string JOB_LOG_FILE = "TransactionServer.log";

        public GAT1400Service()
        {
            m_workDirectory = System.Windows.Forms.Application.StartupPath.ToString();
        }

        private void OnAPEInfoUpdated(object sender, APEArgs e)
        {
            if (null != APEInfoUpdateEvent)
            {
                this.APEInfoUpdateEvent(sender, e);
            }
        }

        private void PrintLog(string text)
        {
            if (m_bTraceLogEnabled)
            {
                Trace.WriteLine(text);
            }
            if (m_bPrintLogEnabled)
            {
                ServiceTools.WriteLog(m_workDirectory + @"\" + JOB_LOG_FILE, text, true);
            }
        }

        private bool LoadRegInfo(string filename, string format)
        {
            string path = m_workDirectory + @"\" + filename + format;
            return ImportFile(CONFIG_TYPE.REGISTRATION, path, FILE_FORMAT);
        }

        private bool LoadAPEInfo(string filename, string format)
        {
            string path = m_workDirectory + @"\" + filename + format;
            return ImportFile(CONFIG_TYPE.DEVICEINFO, path, FILE_FORMAT);
        }


        public ResponseStatus RegisterDevice(Register info)
        {
            WebOperationContext ctx = WebOperationContext.Current;
            IncomingWebRequestContext inRequest = ctx.IncomingRequest;
            OutgoingWebResponseContext outResponse = ctx.OutgoingResponse;
            string header = inRequest.Headers[HttpRequestHeader.Authorization];
            string method = inRequest.Method;
            if (null == header)
            {
                RequestAuthorization(outResponse, ++m_nonce);
                return null;
            }

            string deviceId = info.RegisterObject.DeviceID;
            string username = string.Empty;
            string password = string.Empty;
            if (!IsValidRegister(deviceId, ref username, ref password))
            {
                RequestAuthorization(outResponse, ++m_nonce);
                return null;
            }

            if (!IsValidAuthorization(header, method, username, password))
            {
                RequestAuthorization(outResponse, ++m_nonce);
                return null;
            }

            // add device

            return AcceptRequest(inRequest, outResponse);
        }

        public ResponseStatus UnRegisterDevice(UnRegister info)
        {
            WebOperationContext ctx = WebOperationContext.Current;
            IncomingWebRequestContext inRequest = ctx.IncomingRequest;
            OutgoingWebResponseContext outResponse = ctx.OutgoingResponse;

            string deviceId = info.UnRegisterObject.DeviceID;
            // remove device

            return AcceptRequest(inRequest, outResponse);
        }

        public ResponseStatus KeepAlive(Keepalive info)
        {
            WebOperationContext ctx = WebOperationContext.Current;
            IncomingWebRequestContext inRequest = ctx.IncomingRequest;
            OutgoingWebResponseContext outResponse = ctx.OutgoingResponse;

            string deviceId = info.KeepaliveObject.DeviceID;
            GetDevice(deviceId);

            return AcceptRequest(inRequest, outResponse);
        }

        public void GetDevice(string devId)
        {
            bool ret = LoadAPEInfo(APEINFO_FILENAME, FILE_FORMAT);
            if ((!ret) || (!m_mapAPE.ContainsKey(devId)))
            {
                PrintLog(String.Format("APE info is inavailable for Device(id={0})", devId));
                return;
            }
            APEInfo info = m_mapAPE[devId];
            this.OnAPEInfoUpdated(this, new APEArgs(this, info));
        }

        public bool WebHttpGet(string url, string postData, out string result)
        {
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url + (postData == "" ? "" : "?") + postData);
                httpWebRequest.Method = "GET";
                httpWebRequest.ContentType = "text/html;charset=UTF-8";

                WebResponse webResponse = httpWebRequest.GetResponse();
                HttpWebResponse httpWebResponse = (HttpWebResponse)webResponse;
                System.IO.Stream stream = httpWebResponse.GetResponseStream();
                System.IO.StreamReader streamReader = new System.IO.StreamReader(stream, Encoding.GetEncoding("UTF-8"));
                result = streamReader.ReadToEnd();
                streamReader.Close();
                stream.Close();
                return true;
            }
            catch (Exception ex)
            {
                result = ex.Message;
                return false;
            }
        }

        private ResponseStatus AcceptRequest(IncomingWebRequestContext inRequest, OutgoingWebResponseContext outResponse)
        {
            string template = inRequest.UriTemplateMatch.Template.ToString();
            string uri = template.IndexOf('/') == 0 ? template : "/" + template;
            long timestamp = ((int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds);

            outResponse.StatusCode = HttpStatusCode.OK;
            ResponseStatus status = new ResponseStatus();
            ResponseStatusObject statesObject = new ResponseStatusObject();
            statesObject.RequestURL = uri;
            statesObject.StatusCode = "0";
            statesObject.StatusString = string.Empty;
            statesObject.LocalTime = timestamp;
            status.ResponseStatusObject = statesObject;
            return status;
        }

        private void RequestAuthorization(OutgoingWebResponseContext outResponse, int nonce)
        {
            outResponse.StatusCode = HttpStatusCode.Unauthorized;
            RFC2617AuthHeader authHeader = new RFC2617AuthHeader(RFC2617AuthorizationType.AUTHORIZATION_TYPE_DIGEST, "GAT1400", "auth", "MD5");
            authHeader.Set(RFC2617AuthorizationData.AUTHORIZATION_NONCE, authHeader.EncodeToNonce(string.Format("szNonce:{0}", nonce)));
            string auth = RFC2617Authorization.GenerateWwwAuthenticate(authHeader);
            outResponse.Headers.Set(HttpResponseHeader.WwwAuthenticate, auth);
        }

        private bool IsValidAuthorization(string header, string method, string username, string password)
        {
            RFC2617AuthHeader authHeader = RFC2617Authorization.GenerateAuthHeader(header);
            return authHeader == null ? false : RFC2617Authorization.CheckAuthHeader(authHeader, method, username, password);
        }

        private bool IsValidRegister(string id, ref string username, ref string password)
        {
            bool ret = LoadRegInfo(REGINTO_FILENAME, FILE_FORMAT);
            if ((!ret) || (!m_mapRegistration.ContainsKey(id)))
            {
                PrintLog(String.Format("Regist info is inavailable for Device(id={0})", id));
                return false;
            }

            string[] parts = m_mapRegistration[id].Split(':');
            if (2 != parts.Length)
            {
                PrintLog(String.Format("Invalid userpwd{0} for Device(id={1})", m_mapRegistration[id], id));
                return false;
            }

            username = parts[0];
            password = parts[1];
            return true;
        }

        private bool CheckFileCondition(string sFilename, string sFileFormat)
        {
            if (!File.Exists(sFilename))
            {
                PrintLog("File does not exist: " + sFilename);
                return false;
            }

            if (sFileFormat.ToLower() != Path.GetExtension(sFilename).ToLower())
            {
                PrintLog("Invalid file format : " + sFilename);
                return false;
            }

            return true;
        }

        public void ParseFile(CONFIG_TYPE type, string content)
        {
            string[] lines = content.Split('\n');
            foreach (string line in lines)
            {
                if ((string.Empty == line) || ("\r" == line))
                {
                    PrintLog(String.Format("Invalid line [{0}], skip...\n", line.Trim()));
                    continue;
                }

                string log = string.Empty;
                log += String.Format("Parse line[{0}] : ", line.Trim());
                string[] data = line.Trim().Split(',');
                bool isProcessed = false;
                switch (type)
                {
                    case CONFIG_TYPE.REGISTRATION:

                        RegInfo regInfo = new RegInfo("root", "pass");
                        isProcessed = regInfo.Process(data);
                        if (!isProcessed)
                        {
                            log += "Failed";
                            PrintLog(log);
                            continue;
                        }
                        log += "Successfully";
                        if (m_mapRegistration.ContainsKey(regInfo.m_deviceId))
                        {
                            log += " -> not added";
                            PrintLog(log);
                            continue;
                        }
                        m_mapRegistration.Add(regInfo.m_deviceId, String.Format("{0}:{1}", regInfo.m_username, regInfo.m_password));
                        log += " -> added";
                        PrintLog(log);
                        break;

                    case CONFIG_TYPE.DEVICEINFO:

                        APEInfo apeInfo = new APEInfo();
                        isProcessed = apeInfo.Process(data);
                        if (!isProcessed)
                        {
                            log += "Failed";
                            PrintLog(log);
                            continue;
                        }
                        log += "Successfully";
                        if (m_mapAPE.ContainsKey(apeInfo.m_deviceId))
                        {
                            log += " -> not added";
                            PrintLog(log);
                            continue;
                        }
                        m_mapAPE.Add(apeInfo.m_deviceId, apeInfo);
                        log += " -> added";
                        PrintLog(log);
                        break;

                    default:
                        break;
                }
            }
        }

        private bool ImportFile(CONFIG_TYPE type, string path, string format)
        {
            try
            {
                if (!CheckFileCondition(path, format))
                {
                    PrintLog("Invalid file : " + path);
                    return false;
                }

                switch (type)
                {
                    case CONFIG_TYPE.REGISTRATION:

                        m_mapRegistration.Clear();
                        break;

                    case CONFIG_TYPE.DEVICEINFO:

                        m_mapAPE.Clear();
                        break;

                    default:
                        PrintLog("Unsupported config type : " + type.ToString());
                        return false;
                }

                Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(stream);

                string content = sr.ReadToEnd();
                stream.Close();
                if (string.Empty == content)
                {
                    PrintLog("Empty file : " + path);
                    return false;
                }
                ParseFile(type, content);
                return true;
            }
            catch (Exception ex)
            {
                PrintLog(String.Format("Error importing file \"{0}\" : {1}", path, ex.ToString()));
                return false;
            }
        }

    }

    [DataContract]
    public class Keepalive
    {
        [DataMember]
        public KeepaliveInfo KeepaliveObject { get; set; }
    }

    [DataContract]
    public class KeepaliveInfo
    {
        [DataMember(IsRequired = true)]
        public string DeviceID { get; set; }
    }

    [DataContract]
    public class Register
    {
        [DataMember]
        public RegisterInfo RegisterObject { get; set; }
    }

    [DataContract]
    public class RegisterInfo
    {
        [DataMember(IsRequired = true)]
        public string DeviceID { get; set; }
    }

    public class UnRegister
    {
        [DataMember]
        public UnRegisterInfo UnRegisterObject { get; set; }
    }

    [DataContract]
    public class UnRegisterInfo
    {
        [DataMember(IsRequired = true)]
        public string DeviceID { get; set; }
    }

    [DataContract]
    public class ResponseStatus
    {
        [DataMember]
        public ResponseStatusObject ResponseStatusObject { get; set; }
    }

    [DataContract]
    public class ResponseStatusObject
    {
        [DataMember]
        public string RequestURL { get; set; }

        [DataMember]
        public string StatusCode { get; set; }

        [DataMember]
        public string StatusString { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public long LocalTime { get; set; }
    }


    [DataContract]
    public class APE
    {
        [DataMember]
        public int ID { get; set; }
    }


    [ServiceContract(Name = "GAT1400Service")]
    public interface IServiceCom
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "VIID/System/Register", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseStatus RegisterDevice(Register info);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "VIID/System/Keepalive", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseStatus KeepAlive(Keepalive info);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "VIID/System/UnRegister", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ResponseStatus UnRegisterDevice(UnRegister info);
    }

    public class APEArgs : EventArgs
    {
        public object Object { get; } = null;
        public APEInfo Info { get; } = null;

        public APEArgs(object obj, APEInfo info)
        {
            this.Object = obj;
            this.Info = info;
        }
    }
}
