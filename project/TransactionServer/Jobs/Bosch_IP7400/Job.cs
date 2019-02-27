﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.ComponentModel;
using System.Threading;
using TransactionServer.Base;

namespace TransactionServer.Jobs.Bosch.IP7400
{
    public class Job : Base.ServiceJob
    {
        private string m_jobName = string.Empty;
        private string m_revIp = string.Empty;
        private string m_revPort = string.Empty;
        private string m_devIp = string.Empty;
        private string m_devPort = string.Empty;
        private int m_Interval = 0;
        public static Mutex m_mutex = new Mutex();
        private IntPtr m_pObject = IntPtr.Zero;
        private bool m_bStartRev = false;
        private bool bStartCtl = false;
        private XDocument m_xd = null;
        private static Dictionary<string, string> m_events = null;
        private string m_workDirectory = string.Empty;
        private bool m_bTraceLogEnabled = true;
        private bool m_bPrintLogEnabled = false;
        private bool m_bLoadConfiguration = false;

        private const string OWNER = "Bosch.IP7400";
        private const string RECEIVE_PORT = "7700";
        private const char RECEIVE_ADDR_SEPARATOR = '|';
        private const int LINK_INTERVAL = 70;
        private const string CONFIG_FILE = "transaction.conf";
        private const string JOB_LOG_FILE = "TransactionServer.log";


        public enum EventType
        {
            [Description("未知")]
            EVENT_UNKNOWN = 0,
            [Description("连接正常")]
            COMMUNICATION_SUCCESS = 1,  // 0000
            [Description("连接超时")]
            COMMUNICATION_FAIL = 2, // EXCEED_INTERVAL
            [Description("布防")]
            ALARM_ARM = 3,  // 3401
            [Description("撤防")]
            ALARM_DISARM = 4,   // 1401
            [Description("窃警")]
            ALARM_BURGLARY = 5, // 1130
            [Description("火警")]
            ALARM_FIRE = 6, // 1110
            [Description("紧急按钮")]
            ALARM_EMERGENCY = 7,    // 1120
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

        protected override void Init()
        {
            m_bPrintLogEnabled = (ServiceTools.GetAppSetting("print_log_enabled").ToLower() == "true") ? true : false;
            Config config = this.ConfigObject as Config;
            m_jobName = config.Description;
            m_revIp = config.ReceiveIp;
            m_revPort = config.ReceivePort;
            m_devIp = config.DeviceIp;
            m_devPort = config.DevicePort;
            m_Interval = LINK_INTERVAL;
            m_workDirectory = System.Windows.Forms.Application.StartupPath.ToString();
            m_bLoadConfiguration = LoadConfiguration(m_workDirectory + @"\" + CONFIG_FILE) && (null != m_xd);
        }

        protected override void Cleanup()
        {
            m_jobName = string.Empty;
            m_revIp = string.Empty;
            m_revPort = string.Empty;
            m_devIp = string.Empty;
            m_devPort = string.Empty;
            m_Interval = 0;
            m_pObject = IntPtr.Zero;
            m_bStartRev = false;
            bStartCtl = false;
            m_bPrintLogEnabled = false;
            m_bTraceLogEnabled = false;
            m_xd = null;
            m_events = null;
            m_workDirectory = string.Empty;
            m_bLoadConfiguration = false;
        }

        protected override void Start()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                this.m_IsRunning = true;

                if (string.Empty == m_revIp)
                {
                    m_revIp = GetLocalIpAddress();
                    PrintLog(String.Format("{0} - {1} : receive_ip has not been set, using local ip address[{2}] instead", m_jobName, methodName, m_revIp));
                }
                if (string.Empty == m_revPort)
                {
                    m_revPort = RECEIVE_PORT;
                    PrintLog(String.Format("{0} - {1} : receive_port has not been set, using default port[{2}] instead", m_jobName, methodName, m_revPort));
                }

                Job.m_mutex.WaitOne();

                m_pObject = testBS7400Ctl.New_Object();
                if (IntPtr.Zero == m_pObject)
                {
                    PrintLog(String.Format("{0} - {1} : failed with null object", m_jobName, methodName));
                    Stop();
                    return;
                }

                string rev_addr = m_revIp + RECEIVE_ADDR_SEPARATOR + m_revPort;
                OpenComm(rev_addr, null, out m_bStartRev, out bStartCtl);
                PrintLog(String.Format("{0} - {1} : {2} start receiver({3}) and start controller({4})", m_jobName, methodName, rev_addr, m_bStartRev.ToString(), bStartCtl.ToString()));
                if (m_bStartRev && bStartCtl)
                {
                    Stop();
                    return;
                }
                testBS7400Ctl.SetLnkIntval(m_pObject, m_Interval);

                Job.m_mutex.ReleaseMutex();

                PrintLog(String.Format("{0} - {1} : successful", m_jobName, methodName));
            }
            catch (Exception error)
            {
                this.m_IsRunning = false;
                PrintLog(String.Format("{0} - {1} : failed with exception \"{2}\"", m_jobName, methodName, error.ToString()));
                throw error;
            }
            finally
            {
                //
            }
        }

        protected override void Stop()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            PrintLog(String.Format("{0} - {1} : start", m_jobName, methodName));

            Job.m_mutex.WaitOne();

            CloseComm();
            if (IntPtr.Zero != m_pObject)
            {
                testBS7400Ctl.Delete_Object(m_pObject);
                m_pObject = IntPtr.Zero;
            }

            Job.m_mutex.ReleaseMutex();

            this.m_IsRunning = false;
            PrintLog(String.Format("{0} - {1} : end", m_jobName, methodName));
        }

        protected override void Callback_JobEventSend(object sender, JobEventArgs e)
        {
            //
        }

        private bool LoadConfiguration(string filePath)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string log = string.Empty;

            if (!File.Exists(m_workDirectory + @"\" + CONFIG_FILE))
            {
                log = string.Format("{0} - {1} : failed under [{2}] (file not exists)", m_jobName, methodName, filePath);
                PrintLog(log);
                return false;
            }
            else
            {
                m_xd = XDocument.Load(filePath);
                m_events = GetEventMap(m_xd.Root, "Bosch.IP7400");
                log = string.Format("{0} - {1} : successful under [{2}]", m_jobName, methodName, filePath);
                PrintLog(log);
                return true;
            }
        }

        private string GetLocalIpAddress()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry localHost = Dns.GetHostEntry(hostName);
                foreach (IPAddress ip in localHost.AddressList)
                {
                    if (AddressFamily.InterNetwork == ip.AddressFamily)
                    {
                        return ip.ToString();
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                string log = String.Format("{0} : fail to get local ip address ({1})", m_jobName, ex.Message);
                PrintLog(log);
                return string.Empty;
            }
        }

        private void OpenComm(string rev_addr, string ctl_addr, out bool bStartRev, out bool bStartCtl)
        {
            testBS7400Ctl.ArrangeRcvAddress(m_pObject, rev_addr, ctl_addr);
            testBS7400Ctl.callback = new CallbackDelegate(TranFunc);
            int ret = testBS7400Ctl.OpenReceiver(m_pObject, testBS7400Ctl.callback, IntPtr.Zero);
            int canReceiveEvent = testBS7400Ctl.CanReceiveEvent(m_pObject);
            int canControlPanel = testBS7400Ctl.CanControlPanel(m_pObject);
            bStartRev = ((ret == 0) || (ret == 7)) && (1 == canReceiveEvent);
            bStartCtl = ((ret == 0) || (ret == 11)) && (1 == canControlPanel);
        }

        private void CloseComm()
        {
            if (IntPtr.Zero != m_pObject)
            {
                testBS7400Ctl.CloseReciever(m_pObject);
            }
        }

        public void TranFunc(IntPtr pObject, string sRcvData, int iDataLen)
        {
            string data = sRcvData;

            if ((null != pObject) && (0 < iDataLen) && (null != sRcvData))
            {
                string pattern = "<" + "([^>]*)" + ">";
                ParseData(sRcvData, iDataLen, pattern);
            }
        }

        private List<string> GetElements(string sData, string sPattern)
        {
            List<string> elements = new List<string>();

            Regex r = new Regex(sPattern, RegexOptions.IgnoreCase);
            MatchCollection mc = r.Matches(sData);
            int match_count = mc.Count;
            for (int i = 0; i < match_count; i++)
            {
                Match match = mc[i];
                if (match.Success)
                {
                    string element = GetElement(match);
                    elements.Add(element);
                }
            }

            return elements;
        }

        private string GetElement(Match match)
        {
            string element = string.Empty;

            GroupCollection gc = match.Groups;
            int gc_count = gc.Count;
            if (gc_count > 0)
            {
                Group g = gc[gc_count - 1];
                CaptureCollection collection = g.Captures;
                int capture_count = collection.Count;
                if (capture_count > 0)
                {
                    Capture capture = collection[capture_count - 1];
                    element = capture.ToString();
                }
            }

            return element;
        }

        /*
         * <192.168.77.244|7700><18:00:00><1234><0000><00><000>
         * <192.168.77.244|7700><18:00:00><EXCEED_INTERVAL>
         * <192.168.77.244|7700><18:00:00><1234><EEEE><PP><ZZZ>
         * EEEE : event code (1130 / 1110 / 1120 / 3401 / 1401)
         * PP : partition number
         * ZZZ : zone number
         */
        private void ParseData(string sRcvData, int iDataLen, string sPattern)
        {
            string message = string.Empty;
            Regex regex = new Regex(sPattern);
            MatchCollection matchCollection = regex.Matches(sRcvData);
            int match_count = matchCollection.Count;
            if ((3 != match_count) && (6 != match_count))
            {
                message = string.Format("{0} - [{1}][{2}] : invalid format for receive data)", m_jobName, sRcvData, match_count);
                PrintLog(message);
                return;
            }

            string ip = string.Empty;
            string port = string.Empty;
            int alarm_time = -1;
            string event_desc = string.Empty;
            int part_no = -1;
            int zone_no = -1;

            List<string> elements = GetElements(sRcvData, sPattern);
            string[] addr = elements[0].Split(RECEIVE_ADDR_SEPARATOR);
            if (2 == addr.Length)
            {
                ip = addr[0];
                port = addr[1];
            }
            alarm_time = GetTimeStamp(elements[1]);
            message = String.Format("ip = {0}, port = {1}, time = {2}", ip, port, alarm_time);

            switch (match_count)
            {
                case 3:

                    event_desc = GetEventDesc(GetEventType(elements[2]));
                    message = String.Format("{0}, event = {1}", message, event_desc);
                    PrintLog(m_jobName + " - " + message);

                    return;

                case 6:

                    EventType event_type = GetEventType(elements[3]);
                    message = String.Format("{0}, event = {1}", message, event_desc = GetEventDesc(event_type));
                    if (EventType.COMMUNICATION_SUCCESS == event_type)
                    {
                        PrintLog(m_jobName + " - " + message + " : polling");
                        return;
                    }

                    bool isValidPartitiion = int.TryParse(elements[4], out part_no);
                    bool isValidZone = int.TryParse(elements[5], out zone_no);
                    if (!isValidPartitiion || !isValidZone)
                    {
                        PrintLog(m_jobName + " - " + message + " : invalid part_no or zone_no");
                        return;
                    }

                    message = String.Format("{0}, partition = {1}, zone = {2}", message, part_no, zone_no);
                    PrintLog(m_jobName + " - " + message);

                    if ((m_bLoadConfiguration) && (null != this.m_parentJob) && (this.m_parentJob.IsRunning))
                    {
                        JobEventInfo info = new JobEventInfo(alarm_time);
                        MakeEvent(m_xd.Root, event_desc, zone_no, ref info);
                        if ((-1 == info.event_time) || (-1 == info.camera_id) || (-1 == info.policy_id))
                        {
                            PrintLog(m_jobName + " : unmatched event - " + event_desc);
                            return;
                        }
                        PrintLog(String.Format("{0} ready to send to {1} - message = {2} => event_time = {3}, camera_id = {4}, policy_id = {5}", m_jobName, m_parentJob.ConfigObject.Description, message, info.event_time, info.camera_id, info.policy_id));
                        this.OnJobEventSend(this, new JobEventArgs(this, message, info));
                    }

                    break;

                default:
                    PrintLog(m_jobName + " - " + message + " : invalid data format");
                    return;
            }
        }


        private bool IsTime(string timeval)
        {
            return Regex.IsMatch(timeval, @"^((([0-1]?[0-9])|(2[0-3])):([0-5]?[0-9])(:[0-5]?[0-9])?)$");
        }

        private int GetTimeStamp(string time)
        {
            int timestamp = -1;

            bool isTimeFormat = IsTime(time);
            if (isTimeFormat)
            {
                DateTime dt = Convert.ToDateTime(time);
                timestamp = ((int)(Convert.ToDateTime(time) - new DateTime(1970, 1, 1)).TotalSeconds);
            }

            return timestamp;
        }

        private string GetEventDesc(EventType type)
        {
            FieldInfo fieldInfo = type.GetType().GetField(type.ToString());
            DescriptionAttribute attr = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute), false) as DescriptionAttribute;
            return attr.Description;
        }

        private EventType GetEventType(string event_code)
        {
            EventType type = EventType.EVENT_UNKNOWN;

            switch (event_code)
            {
                case "0000": type = EventType.COMMUNICATION_SUCCESS; break;
                case "EXCEED_INTERVAL": type = EventType.COMMUNICATION_FAIL; break;
                case "3401": type = EventType.ALARM_ARM; break;
                case "1401": type = EventType.ALARM_DISARM; break;
                case "1130": type = EventType.ALARM_BURGLARY; break;
                case "1110": type = EventType.ALARM_FIRE; break;
                case "1120": type = EventType.ALARM_EMERGENCY; break;
                default: type = EventType.EVENT_UNKNOWN; break;
            }

            return type;
        }

        private Dictionary<string, string> GetEventMap(XElement element, string owner)
        {
            Dictionary<string, string> events = new Dictionary<string, string>();

            var eEvents = from item in element.Descendants("event_map")
                          where item.Attribute("owner").Value == owner
                          select item;
            eEvents.ToList().ForEach(it =>
            {
                events.Clear();
                foreach (XElement e in it.Elements("event"))
                {
                    string[] inputEvents = e.Attribute("desc").Value.Split(',');
                    foreach (string inputEvent in inputEvents)
                    {
                        if (!events.ContainsKey(inputEvent))
                        {
                            events.Add(inputEvent, e.Value);
                        }
                    }
                }
            });

            return events;
        }

        private void MakeEvent(XElement element, string event_desc, int zone_no, ref JobEventInfo info)
        {
            if (!m_events.ContainsKey(event_desc))
            {
                PrintLog(String.Format("{0} - {1} : undefined event({2})", m_jobName, MethodBase.GetCurrentMethod().Name, event_desc));
                return;
            }
            var ePolicyMap = from item in element.Descendants("policy_map")
                             where item.Attribute("owner").Value == OWNER
                             select item;
            if (null != ePolicyMap)
            {
                var ePolicy = (from item in ePolicyMap.Descendants("policy")
                               where item.Attribute("devIp").Value == m_devIp
                               && item.Attribute("devPort").Value == m_devPort
                               && item.Attribute("event").Value == m_events[event_desc]
                               && item.Attribute("zone").Value == zone_no.ToString()
                               select item).FirstOrDefault();
                if (null != ePolicy)
                {
                    info.camera_id = int.Parse(ePolicy.Attribute("camId").Value);
                    info.policy_id = int.Parse(ePolicy.Value);
                }
            }
        }

    }


    public delegate void CallbackDelegate(IntPtr pObject, string sRcvData, int iDataLen);
    public class testBS7400Ctl
    {
        //public delegate void CallbackDelegate(IntPtr m_pObject, string sRcvData, int iDataLen);
        public static CallbackDelegate callback;
        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "OpenReceiver")]
        public static extern int OpenReceiver(IntPtr pObject, CallbackDelegate pTranFunc, IntPtr pPara);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLnkIntval(IntPtr pObject, int iInterval);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr New_Object();

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Delete_Object(IntPtr pObject);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ArrangeRcvAddress(IntPtr pObject, string pRcvAddress, string pCtrlAddress);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Execute(IntPtr pObject, string sIPAdress, string sCommand, string sPara, int iPanelType);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseReciever(IntPtr pObject);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CanControlPanel(IntPtr pObject);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CanReceiveEvent(IntPtr pObject);

        [DllImport("BS7400Ctl.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetPanelControlCodes(IntPtr pObject, string sPanelAddress, string sAgencyCode, string sPasscode);
    }
}
