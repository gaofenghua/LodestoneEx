using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Linq;

using TransactionServer.Base;

namespace TransactionServer.Jobs.Bosch.IP7400
{
    public class Job : Base.ServiceJob
    {
        public delegate void JobEventHandler(object sender, JobEventArgs e);    // agent
        public event JobEventHandler JobEventSend;  // event

        string m_revIp = string.Empty;
        string m_revPort = string.Empty;
        int m_Interval = 0;

        private IntPtr pObject = IntPtr.Zero;
        private bool bStartRev = false;
        private bool bStartCtl = false;

        private const string RECEIVE_IP = "192.168.77.244";
        private const string RECEIVE_PORT = "7700";
        private const char RECEIVE_ADDR_SEPARATOR = '|';  // : is also ok
        private const int LINK_INTERVAL = 70;   // polling_interval is 65s

        public enum EventType
        {
            EVENT_UNKNOWN = 0,
            COMMUNICATION_SUCCESS = 1,  // 0000
            COMMUNICATION_FAIL = 2, // EXCEED_INTERVAL
            ALARM_ARM = 3,  // 3401
            ALARM_DISARM = 4,   // 1401
            ALARM_BURGLARY = 5, // 1130
            ALARM_FIRE = 6, // 1110
            ALARM_EMERGENCY = 7,    // 1120
        }

        public void OnJobEventSend(object sender, JobEventArgs e)
        {
            if (null != JobEventSend)
            {
                this.JobEventSend(sender, e);
            }
        }

        private void PrintLog(string text)
        {
            //if (!m_bPrintLogAllowed)
            //{
            //    return;
            //}
            //ServiceTools.WriteLog(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + JOB_LOG_FILE, text, true);

            Trace.WriteLine(text);
        }

        protected override void Init()
        {
            m_revIp = RECEIVE_IP;
            m_revPort = RECEIVE_PORT;
            m_Interval = LINK_INTERVAL;
            pObject = testBS7400Ctl.New_Object();
        }

        protected override void Cleanup()
        {
            pObject = IntPtr.Zero;
            bStartRev = false;
            bStartCtl = false;
    }

        protected override void Start()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                this.m_IsRunning = true;

                Stop();
                pObject = testBS7400Ctl.New_Object();
                if (IntPtr.Zero == pObject)
                {
                    this.m_IsRunning = false;
                    PrintLog(String.Format("{0} : failed with null object", methodName));
                    ServiceTools.WindowsServiceStop("TransactionServer");
                    return;
                }
                if (bStartRev || bStartCtl)
                {
                    CloseComm();
                }
                string rev_addr = m_revIp + RECEIVE_ADDR_SEPARATOR + m_revPort;
                OpenComm(rev_addr, null, out bStartRev, out bStartCtl);

                testBS7400Ctl.SetLnkIntval(pObject, m_Interval);

            }
            catch (Exception error)
            {
                this.m_IsRunning = false;
                PrintLog(String.Format("{0} : failed with exception \"{1}\"", methodName, error.ToString()));
                ServiceTools.WindowsServiceStop("TransactionServer");
            }
            finally
            {
                PrintLog(String.Format("{0} : successful", methodName));
            }
        }

        protected override void Stop()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            PrintLog(methodName + " start");

            CloseComm();
            if (IntPtr.Zero != pObject)
            {
                testBS7400Ctl.Delete_Object(pObject);
                pObject = IntPtr.Zero;
            }

            this.m_IsRunning = false;

            PrintLog(methodName + " end");
        }


        private void OpenComm(string rev_addr, string ctl_addr, out bool bStartRev, out bool bStartCtl)
        {
            //testBS7400Ctl.ArrangeRcvAddress(pObject, "192.168.77.244|7700", "0.0.0.0|000"); // pCtlAddress : null or 0.0.0.0|XXX
            testBS7400Ctl.ArrangeRcvAddress(pObject, rev_addr, ctl_addr);

            //testBS7400Ctl.CallbackDelegate TranDataProcDelegate = new testBS7400Ctl.CallbackDelegate(TranFunc);
            //int ret = testBS7400Ctl.OpenReceiver(pObject, TranDataProcDelegate, IntPtr.Zero);
            // extend delegate field
            testBS7400Ctl.callback = new CallbackDelegate(TranFunc);
            int ret = testBS7400Ctl.OpenReceiver(pObject, testBS7400Ctl.callback, IntPtr.Zero);

            int canReceiveEvent = testBS7400Ctl.CanReceiveEvent(pObject);
            int canControlPanel = testBS7400Ctl.CanControlPanel(pObject);
            bStartRev = ((ret == 0) || (ret == 7)) && (1 == canReceiveEvent);
            bStartCtl = ((ret == 0) || (ret == 11)) && (1 == canControlPanel);
        }

        private void CloseComm()
        {
            if (IntPtr.Zero != pObject)
            {
                testBS7400Ctl.CloseReciever(pObject);
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
                    //Console.WriteLine(String.Format("Found match-{0} at position-{2} : {3}", i + 1, match.Success, match.Index, match.Value));
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
            //Console.WriteLine(sRcvData);
            Trace.WriteLine(sRcvData);

            Regex regex = new Regex(sPattern);
            MatchCollection matchCollection = regex.Matches(sRcvData);
            int match_count = matchCollection.Count;
            List<string> elements = GetElements(sRcvData, sPattern);

            string addr = string.Empty;
            string alarm_time = string.Empty;
            string password = string.Empty;
            string event_code = string.Empty;
            string part_no = string.Empty;
            string zone_no = string.Empty;
            EventType event_type = EventType.EVENT_UNKNOWN;
            switch (match_count)
            {
                case 3:

                    if (elements[0].Contains(RECEIVE_ADDR_SEPARATOR))
                    {
                        addr = elements[0];
                    }

                    alarm_time = GetDateTime(elements[1]);

                    event_code = elements[2];
                    event_type = GetEventType(event_code);

                    //Console.WriteLine(String.Format("ip = {0}, port = {1}, time = {2}, event = {3}",
                    //    addr.Split(RECEIVE_ADDR_SEPARATOR)[0],
                    //    addr.Split(RECEIVE_ADDR_SEPARATOR)[1],
                    //    alarm_time,
                    //    event_type.ToString())
                    //    );
                    string msg3 = String.Format("ip = {0}, port = {1}, time = {2}, event = {3}",
                        addr.Split(RECEIVE_ADDR_SEPARATOR)[0],
                        addr.Split(RECEIVE_ADDR_SEPARATOR)[1],
                        alarm_time,
                        event_type.ToString());
                    //Trace.WriteLine(msg3);
                    this.OnJobEventSend(this, new JobEventArgs(this, msg3));

                    break;

                case 4:

                    // reserve for "QUERY_7400_STATUS"
                    break;

                case 6:

                    if (elements[0].Contains(RECEIVE_ADDR_SEPARATOR))
                    {
                        addr = elements[0];
                    }

                    alarm_time = GetDateTime(elements[1]);

                    password = elements[2];

                    event_code = elements[3];
                    event_type = GetEventType(event_code);

                    bool isValid = Regex.IsMatch(elements[4], @"^\d{2}$");
                    if (isValid)
                    {
                        part_no = elements[4];
                    }

                    isValid = Regex.IsMatch(elements[5], @"^\d{3}$");
                    if (isValid)
                    {
                        zone_no = elements[5];
                    }

                    //Console.WriteLine(String.Format("ip = {0}, port = {1}, time = {2}, password = {3}, event = {4}",
                    //    addr.Split(RECEIVE_ADDR_SEPARATOR)[0],
                    //    addr.Split(RECEIVE_ADDR_SEPARATOR)[1],
                    //    password,
                    //    alarm_time,
                    //    event_type.ToString())
                    //    );
                    string msg6 = String.Format("ip = {0}, port = {1}, time = {2}, password = {3}, event = {4}",
                        addr.Split(RECEIVE_ADDR_SEPARATOR)[0],
                        addr.Split(RECEIVE_ADDR_SEPARATOR)[1],
                        password,
                        alarm_time,
                        event_type.ToString());
                    if (event_type != EventType.COMMUNICATION_SUCCESS)
                    {
                        msg6 += String.Format(", partition = {0}, zone = {1}", part_no, zone_no);
                    }
                    //Trace.WriteLine(msg6);
                    this.OnJobEventSend(this, new JobEventArgs(this, msg6));

                    break;

                default:
                    //Console.WriteLine("invalid data format");
                    Trace.WriteLine("invalid data format");
                    return;
            }
        }


        private bool IsTime(string timeval)
        {
            return Regex.IsMatch(timeval, @"^((([0-1]?[0-9])|(2[0-3])):([0-5]?[0-9])(:[0-5]?[0-9])?)$");    // HH:mm:ss
        }

        private string GetDateTime(string time)
        {
            string sDateTime = string.Empty;

            bool isTimeFormat = IsTime(time);
            if (isTimeFormat)
            {
                DateTime dt = Convert.ToDateTime(time);
                sDateTime = dt.ToString();
            }

            return sDateTime;
        }

    }


    public delegate void CallbackDelegate(IntPtr pObject, string sRcvData, int iDataLen);
    public class testBS7400Ctl
    {
        //public delegate void CallbackDelegate(IntPtr pObject, string sRcvData, int iDataLen);
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
