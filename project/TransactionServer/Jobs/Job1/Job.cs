using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
//
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
// customize
using TransactionServer.Base;
// AVMS SDK
using Seer;
using BaseIDL;
using Seer.BaseLibCS;
using Seer.BaseLibCS.Communication;
using Seer.Configuration;
using Seer.Connectivity;
using Seer.Database;
using Seer.Database.BaseLibCS;
using Seer.DeviceModel;
using Seer.DeviceModel.Client;
using Seer.FarmLib.Client;
using Seer.SDK;
using Seer.SDK.NotificationMonitors;
using Seer.Utilities;
// 3rd
using Newtonsoft.Json;


namespace TransactionServer.Jobs.Job1
{
    public class Job : Base.ServiceJob
    {
        private Utils m_utils = new Utils();
        private SdkFarm m_farm = null;
        private AlarmMonitor m_alarmMonitor;
        private ManualResetEvent m_waitForServerInitialized = new ManualResetEvent(false);
        // flag
        private bool m_bConnectedToServer = false;
        private bool m_bStartedListener = false;
        private bool m_bDeviceModelEventHandlerAdded = false;
        private bool m_bListenerEventHandlerAdded = false;
        private bool m_bAcquiredServerList = false;
        private bool m_bAcquiredCameraList = false;
        private bool m_bPrintLogAllowed = false;
        private bool m_bDatabaseAccessAllowed = false;
        // data
        string m_serverIp = string.Empty;
        string m_serverUsername = string.Empty;
        string m_serverPassword = string.Empty;
        Dictionary<uint, string> m_serverList = new Dictionary<uint, string>();
        Dictionary<uint, CCamera> m_cameraList = new Dictionary<uint, CCamera>();
        private AlarmType m_enumAlarmType = AlarmType.UNKNOWN;
        private string m_policyTypeDesc = string.Empty; // XML format
        private Dictionary<int, ArrayList> m_mapActionEvents = null;
        private ArrayList[] m_listActionCommands = null;    // array includes [action_type] and [Command]
        private List<string> m_listEventIds = new List<string>();

        // default
        private const string IP_ADDRESS = "127.0.0.1";  // 192.168.77.244
        private const string USERNAME = "admin";
        private const string PASSWORD = "admin";
        private const string RULE_EVENT_CONFIG = "transaction.conf";
        private const string JOB_LOG_FILE = "TransactionServer_Job1.log";

        public enum AlarmType
        {
            [Description("Unknown")]
            UNKNOWN = -1,
            // according to database
            ALARM = 0,
            OBSTRUCTED = 3,
            NONE = 7,
            CORD_CUT = 8,
            //
            CUSTOMIZED = 12,
        }

        protected override void Init()
        {
            //m_serverIp = IP_ADDRESS;
            //m_serverUsername = USERNAME;
            //m_serverPassword = Utils.EncodeString(PASSWORD);

            //m_bPrintLogAllowed = (ServiceTools.GetAppSetting("job1_log_enabled").ToLower() == "true") ? true : false;
        }

        protected override void Cleanup()
        {
            m_serverIp = string.Empty;
            m_serverUsername = string.Empty;
            m_serverPassword = string.Empty;
            m_serverList.Clear();
            m_serverList.Clear();
            m_policyTypeDesc = string.Empty;
            m_mapActionEvents = null;
            m_listActionCommands = null;
        }

        protected override void Start()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                this.m_IsRunning = true;
                m_utils.CreateWorkerThread("ExecuteLogic", ExecuteLogic);
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
            DestroyFarm();
            this.m_IsRunning = false;
        }

        private void ExecuteLogic()
        {
            ConnectToFarm();
            if (m_bConnectedToServer)
            {
                if (!AcquireAvailableDevices())
                {
                    return;
                }
                // start listener
                ListenToEvent();
            }
        }

        private void PrintLog(string text)
        {
            if (!m_bPrintLogAllowed)
            {
                return;
            }
            ServiceTools.WriteLog(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + JOB_LOG_FILE, text, true);
        }

        private void ListenToEvent()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            m_bStartedListener = false;
            if (!m_bListenerEventHandlerAdded)
            {
                m_alarmMonitor = new AlarmMonitor(m_farm);
                m_alarmMonitor.AlarmReceived += new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                m_bListenerEventHandlerAdded = true;

                PrintLog(String.Format("{0} : success to add event handler", methodName));
            }
            m_bStartedListener = true;
        }

        private void ConnectToFarm()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string sStatus = string.Empty;
            string log = string.Empty;

            PrintLog(methodName + " start");
            try
            {
                DestroyFarm();
                if (string.Empty != (sStatus = LoadFarm()))
                {
                    log = String.Format("{0} : LoadFarm failed [{1}]", methodName, sStatus);
                    Trace.WriteLine(log);
                    PrintLog(log);
                    return;
                }
                m_waitForServerInitialized.Set();   // LoadFarm is ok

                if (!m_waitForServerInitialized.WaitOne(TimeSpan.FromSeconds(60)))
                {
                    log = String.Format("{0} : {1}", methodName, "Server connection established but server did not initialize within 60 seconds");
                    Trace.WriteLine(log);
                    PrintLog(log);
                    DestroyFarm();
                    return;
                }

                m_farm.SetEnabled(true);
                log = String.Format("{0} : LoadFarm successed", methodName);
                Trace.WriteLine(log);
                PrintLog(log);
                m_bConnectedToServer = true;
            }
            catch (Exception ex)
            {
                log = String.Format("{0} : {1}", methodName, "There was an error connecting to the farm: " + ex.ToString());
                Trace.WriteLine(log);
                PrintLog(log);
                DestroyFarm();
            }
            finally
            {
                PrintLog(methodName + " end");
            }
        }

        private void DestroyFarm()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string log = string.Empty;

            PrintLog(methodName + " start");

            if (null != m_farm)
            {
                CDeviceManager deviceManager = m_farm.DeviceManager;
                if (m_bDeviceModelEventHandlerAdded)
                {
                    deviceManager.DataLoadedEvent -= new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent);
                    m_bDeviceModelEventHandlerAdded = false;
                }
                if (m_bListenerEventHandlerAdded)
                {
                    m_alarmMonitor.AlarmReceived -= new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                    m_bListenerEventHandlerAdded = false;
                }
                m_alarmMonitor = null;

                m_farm.SetEnabled(false);
                Thread.Sleep(1000);
                m_farm.Dispose();
                m_farm = null;
                log = String.Format("{0} : DisposeFarm successed", methodName);
                Trace.WriteLine(log);
                PrintLog(log);

                m_bConnectedToServer = false;
                m_bStartedListener = false;
            }
            m_waitForServerInitialized.Reset();

            PrintLog(methodName + " end");
        }

        private string LoadFarm()
        {
            try
            {
                CNetworkAddress address = new CNetworkAddress(m_serverIp);
                m_farm = new SdkFarm(address, m_serverUsername, m_serverPassword);
                m_farm.DeviceModelRefreshTrigger = Seer.FarmLib.Client.CFarm.DeviceAutoRefreshTrigger.AnyChange;

                return m_farm.Connect();
            }
            catch (Exception ex)
            {
                return "Failed to connect to farm: " + ex.Message;
            }
        }

        private bool AcquireAvailableDevices()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string sStatus = string.Empty;

            PopulateServerList();
            if (!m_bAcquiredServerList)
            {
                PrintLog(String.Format("{0} : fail to acquire servers", methodName));
                return false;
            }
            if ((sStatus = RefreshDeviceManager()) != "")
            {
                PrintLog(String.Format("{0} : fail to acquire devices [{1}]", methodName, sStatus));
                return false;
            }
            return true;
        }

        private string[] AttemptConnection()
        {
            string notes = "Please make sure the service \"AI Infoservice\" is running on the server and that it is not firewalled. If authenticating against ActiveDirectory you may need to specify <domain>\\<username> (eg microsoft\\bgates).";

            try
            {
                IPEndPoint[] endPoints = null;
                endPoints = Utils.ToEndPoints(m_serverIp);
                using (ServerConnectionManager scm = ServerConnectionManager.CreateManager(endPoints, Guid.Empty, new EstablishConnectionOptions(0, TimeSpan.FromSeconds(0))))
                {
                    Seer.BaseLibCS.Proxy.Registration.Registration registrationProxy = scm.GetWebServiceProxy<Seer.BaseLibCS.Proxy.Registration.Registration>();    // verify the credentials
                    string[] servers = registrationProxy.GetAddressesOfServers(m_serverUsername, m_serverPassword);
                    return servers;
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new Exception("Not Authorized. Check user name and password\". " + notes);
            }
            catch (Exception ex)
            {
                string message = string.Empty;
                if (0 <= ex.ToString().IndexOf("WebException"))
                {
                    AILog.Log(LogLevels.LogError, ex.ToString());
                    message = string.Format("{0} [{1}]. {2}",
                        "Server is not online or not reachable",
                        m_serverIp,
                        notes);
                }
                else
                {
                    AILog.Log(LogLevels.LogError, ex.ToString());
                    message = string.Format("{0} {1}. {2}",
                        "Error: Could not connect to",
                        m_serverIp,
                        notes);
                }
                throw new Exception(message);
            }
        }

        private void PopulateServerList()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            m_bAcquiredServerList = false;

            if (null == m_farm)
            {
                return;
            }

            try
            {
                string[] servers = AttemptConnection();
                m_serverList.Clear();
                uint id = 0;
                foreach (string server in servers)
                {
                    m_serverList.Add(id, server);
                    PrintLog(String.Format("{0} : m_serverList[serverId={1}, server={2}]", methodName, id, m_serverList[id]));
                    id++;
                }
            }
            catch (Exception ex)
            {
                PrintLog(String.Format("{0} : {1}", methodName, "Failed to connect to farm: " + ex.Message));
            }

            m_bAcquiredServerList = true;
        }

        private void PopulateCameraList()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            m_bAcquiredCameraList = false;

            if (null == m_farm)
            {
                return;
            }

            m_cameraList.Clear();
            foreach (CCamera cam in m_farm.DeviceManager.GetAllCameras())
            {
                uint camId = cam.CameraId;
                m_cameraList.Add(camId, cam);
                PrintLog(String.Format("{0} : m_cameraList[cameraId={1}, camera={2}]", methodName, camId, m_cameraList[camId]));
            }

            m_bAcquiredCameraList = true;
        }

        private string RefreshDeviceManager()
        {
            try
            {
                if (null == m_farm.DeviceManager)
                {
                    return "Failed to access Device Manager. Value null";
                }

                CDeviceManager deviceManager = m_farm.DeviceManager;
                if (!m_bDeviceModelEventHandlerAdded)
                {
                    deviceManager.DataLoadedEvent += new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent);
                    m_bDeviceModelEventHandlerAdded = true;
                }
                deviceManager.Refresh();
            }
            catch (Exception ex)
            {
                return "Failed to refresh device manager: " + ex.ToString();
            }

            return string.Empty;
        }

        private void DeviceManager_DataLoadedEvent(object sender, EventArgs e)
        {
            try
            {
                PopulateCameraList();
            }
            catch (Exception ex)
            {
                // TBD
            }
        }

        private void HandleAlarmMessageReceived(object sender, AlarmMessageEventArgs e)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string log = string.Empty;

            // parse

            CameraMessageStruct cameraMessageStruct = e.Message;
            uint alarm_id = cameraMessageStruct.m_iAlarmDbId;
            uint camera_id = cameraMessageStruct.m_iCameraId;
            uint event_id = cameraMessageStruct.m_iEvent;
            int policy_id = cameraMessageStruct.m_iPolicyId;
            uint alarm_time = cameraMessageStruct.m_utcTime;    // utc time

            log = String.Format("{0} : receive an alarm message [alarm_id={1}, alarm_time={2}, camera_id={3}, alarm_type_id={4}, policy_type_id={5}]",
                                methodName, alarm_id, alarm_time, camera_id, event_id, policy_id);
            Trace.WriteLine(log);
            PrintLog(log);

            // process

            // alarm time (optional)
            DateTime alarm_datetime = new DateTime();
            AdjustTime(ref cameraMessageStruct, out alarm_datetime, true);
            // alarm type (optional)
            string alarm_type = ToAlarmType((int)event_id);
            // policy type (critical)
            string policy_type = ToPolicyType((int)policy_id);

            log = String.Format("{0} : process elements [alarm_id={1}, alarm_datetime={2}, camera_id={3}, alarm_type={4}, policy_type={5}]",
                                methodName, alarm_id, alarm_datetime.ToString(), camera_id, alarm_type, policy_type);
            Trace.WriteLine(log);
            PrintLog(log);

            // filter

            FilterEvents();
            if ((null == m_mapActionEvents) || (0 == m_mapActionEvents.Count))
            {
                return;
            }

            // executer

            // extract commands from mapActionEvents
            ExtractCommand();
            // send commands refer to index order
            SendCommand();
        }

        private void SendCommand()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string log = string.Empty;

            for (int i = 0; i < m_listActionCommands.Count(); i++)
            {
                ArrayList command_detail = m_listActionCommands[i];
                string action_desc = command_detail[0] as string;
                Command action_command = command_detail[1] as Command;
                // method(GET/POST), url, body(if POST)
                string command_id = action_command.command_id;
                string command_desc = action_command.command_desc;
                string method = action_command.command_method;
                string url = action_command.command_url;

                log = String.Format("{0} : command id = {1}, command desc = {2}, method = {3}, url = {4}", methodName, command_id, command_desc, method, url);
                Trace.WriteLine(log);
                PrintLog(log);

                if ((string.Empty == url) && (!url.Contains("http")))
                {
                    Trace.WriteLine("=> Invalid command");
                    continue;
                }

                string status = string.Empty;
                string result = string.Empty;
                bool ret = false;

                CgiFactory factory = new CgiFactory();
                factory.CURL_Init();
                factory.CURL_SetUrl(url);
                if ("GET" == method)
                {
                    factory.CURL_SetMethod(CgiFactory.CURL_METHOD.CURL_METHOD_GET);
                    ret = factory.CURL_HTTP_Get(out status, out result);
                }
                else if ("POST" == method)
                {
                    factory.CURL_SetMethod(CgiFactory.CURL_METHOD.CURL_METHOD_POST);
                    string body = action_command.command_body;
                    string post_data = string.Empty;
                    if (null != body)
                    {
                        post_data = ConvertJsonString(body);
                    }
                    else
                    {
                        BodyElement body_elements = action_command.body_elements;
                        if (null != body_elements)
                        {
                            // construct body text : TBD
                        }
                    }
                    factory.CURL_SetPostData(post_data);
                    ret = factory.CURL_HTTP_Post(out status, out result);
                }
                else
                {
                    log = String.Format("{0} : execute command [{1}]_{2}_[{3}] [{4}]url=[{5}] => Not define method",
                                        methodName, action_desc, command_id, command_desc, method, url);
                    Trace.WriteLine(log);
                    PrintLog(log);
                    continue;
                }

                log = String.Format("{0} : execute command [{1}]_{2}_[{3}] [{4}]\n{5} to Execute [{6}] with result\n{7}",
                                    methodName, action_desc, command_id, command_desc, method, ret ? "Success" : "Failed", status, result);
                Trace.WriteLine(log);
                PrintLog(log);
            }
        }

        private void ExtractCommand()
        {
            m_listActionCommands = new ArrayList[] { };
            uint index = 0;
            int count = 0;
            foreach (int id in m_mapActionEvents.Keys)
            {
                ArrayList actionList = m_mapActionEvents[id];
                for (int i = 0; i < actionList.Count; i++)  // ignore order
                {
                    Action action = actionList[i] as Action;
                    string action_type = action.action_type;
                    List<Command> commands = action.commands;
                    Dictionary<int, ArrayList> dict = new Dictionary<int, ArrayList>();
                    foreach (Command command in commands)
                    {
                        string command_id = command.command_id;
                        string command_desc = command.command_desc;
                        ArrayList detail = new ArrayList();
                        detail.Add(action_type);
                        detail.Add(command);
                        dict.Add(int.Parse(command_id), detail);
                    }
                    // ascending order
                    dict = dict.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
                    // append to action command array
                    count += dict.Count;
                    Array.Resize<ArrayList>(ref m_listActionCommands, count);
                    foreach (int key in dict.Keys)
                    {
                        m_listActionCommands[index] = dict[key];
                        index++;
                    }
                }
            }
        }

        private void FilterEvents()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string log = string.Empty;

            string policy_name = string.Empty;
            string sStatus = string.Empty;
            if (m_bDatabaseAccessAllowed)
            {
                if (string.Empty == m_policyTypeDesc)
                {
                    log = String.Format("{0} : {1}", methodName, "Nothing needs to be filtered!");
                    Trace.WriteLine(log);
                    PrintLog(log);
                    return;
                }

                sStatus = MappingActionEvents(m_policyTypeDesc, out policy_name, out m_mapActionEvents);
            }
            else
            {
                sStatus = MappingActionEvents(out policy_name, out m_mapActionEvents);  // according to event ids
            }

            log = String.Format("{0} : MappingActionEvents status [{1}] with policy name is {2} and event num is {3}", methodName, sStatus, policy_name, m_mapActionEvents.Count);
            Trace.WriteLine(log);
            PrintLog(log);
        }

        private void AdjustTime(ref CameraMessageStruct cameraMessageStruct, out DateTime serverTime, bool isNeedAdjust = false)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            string log = string.Empty;
            serverTime = new DateTime();

            if (!isNeedAdjust)
            {
                return;
            }

            uint camera_id = cameraMessageStruct.m_iCameraId;
            DateTime clientTime = TimeUtils.DateTimeFromUTC(cameraMessageStruct.m_utcTime);
            short timezoneTime = 0;
            if (m_bAcquiredCameraList && (m_cameraList.ContainsKey(camera_id)))
            {
                serverTime = m_cameraList[camera_id].Server.ToLocalTime(clientTime);
                timezoneTime = (short)(clientTime - serverTime).TotalMinutes;
                bool bAdjustTimezone = (cameraMessageStruct.m_timezoneTime != timezoneTime);
                log = String.Format("{0} : timezone {1}", methodName, bAdjustTimezone ? "should been adjusted from [" + cameraMessageStruct.m_timezoneTime + "] to [" + timezoneTime + "]" : "not adjusted with value [" + cameraMessageStruct.m_timezoneTime + "]");
                PrintLog(log);
                if (bAdjustTimezone)
                {
                    cameraMessageStruct.m_timezoneTime = timezoneTime;
                }
            }
            else if (0 < m_cameraList.Count)
            {
                serverTime = m_cameraList[0].Server.ToLocalTime(clientTime);
            }
            else
            {
                serverTime = clientTime;    // ignore
            }
        }

        private string ToAlarmType(int typeId)
        {
            string type = string.Empty;
            
            if (m_bDatabaseAccessAllowed)
            {
                ArrayList list;
                Query("AlarmTypes", "Nm", "Id=" + typeId, out list);
                if (1 == list.Count)
                {
                    type = ((string[])list[0])[0];
                    m_enumAlarmType = (AlarmType)Enum.Parse(typeof(AlarmType), type, true);
                }
                else
                {
                    m_enumAlarmType = AlarmType.UNKNOWN;
                    // get description of m_enumAlarmType
                    Type enumType = m_enumAlarmType.GetType();
                    string name = Enum.GetName(typeof(AlarmType), 1);
                    if (null == name)
                    {
                        return string.Empty;
                    }
                    FieldInfo fieldInfo = enumType.GetField(name);
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                    type = attr.Description;
                }
            }
            else
            {
                type = Enum.GetName(typeof(AlarmType), typeId);
                if (null == type)
                {
                    m_enumAlarmType = AlarmType.UNKNOWN;
                    type = Enum.GetName(typeof(AlarmType), m_enumAlarmType);
                }
                else
                {
                    m_enumAlarmType = (AlarmType)Enum.Parse(typeof(AlarmType), type, true);
                }
            }

            return type;
        }

        private string ToPolicyType(int typeId)
        {
            string type = string.Empty;

            if (m_bDatabaseAccessAllowed)
            {
                ArrayList list;
                Query("Policy", new string[] { "Nm", "XML" }, new string[] { "Id=" + typeId, "Typ='policy'" }, out list);
                if (1 == list.Count)
                {
                    type = ((string[])list[0])[0];
                    m_policyTypeDesc = ((string[])list[0])[1];
                }
                else
                {
                    type = "#NO VALUE";
                    m_policyTypeDesc = string.Empty;
                }
            }
            else
            {
                EventCollection config_list = DeserializeFromXml<EventCollection>(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + RULE_EVENT_CONFIG);
                if ((null == config_list) || (null == config_list.RuleList) || (0 == config_list.RuleList.Count()))
                {
                    type = "#NO VALUE";
                }
                else
                {
                    // get event id via config file
                    m_listEventIds.Clear();
                    foreach (Rule rule in config_list.RuleList)
                    {
                        type = "#NO VALUE";
                        if (typeId.ToString() == rule.rule_id)
                        {
                            type = rule.rule_name;
                            string[] event_ids = rule.event_id.Split(',');
                            foreach (string event_id in event_ids)
                            {
                                if (!m_listEventIds.Contains(event_id))
                                {
                                    m_listEventIds.Add(event_id);
                                }
                            }

                            break;
                        }
                    }
                }
            }

            return type;
        }

        private int GetEventTypeId(string locId)
        {
            int typeId = -1;
            switch (locId)
            {
                case "10":
                    typeId = 1;
                    break;
                case "20":
                    typeId = 2;
                    break;
                case "23":
                    typeId = 4;
                    break;
                default:
                    break;
            }
            return typeId;
        }

        /* xmlText of policy */
        //<policy>
        //	<action>22</action>
        //	<schedule>8</schedule>
        //	<priority>5</priority>
        //	<events>
        //		<type>SEQ</type>
        //		<period>30</period>
        //		<loc seq="0">23</loc>
        //		<loc seq="1">20</loc>
        //  </events>
        //</policy>
        private string MappingActionEvents(string strPolicyXML, out string strPolicyName, out Dictionary<int, ArrayList> mapActionEvents)
        {
            try
            {
                strPolicyName = string.Empty;
                mapActionEvents = null;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(strPolicyXML);
                // action id
                XmlNode actionNode = doc.SelectSingleNode("policy/action");
                if (null == actionNode)
                {
                    return "Invalid XML format (not found action node)";
                }
                string action_id = actionNode.InnerText;
                // event id list
                XmlNode eventsNode = doc.SelectSingleNode("policy/events");
                if (null == eventsNode)
                {
                    return "Invalid XML format (not found events node)";
                }
                XmlNodeList itemNodeList = eventsNode.ChildNodes;
                if (null == itemNodeList)
                {
                    return "Invalid XML format (no content included in events node)";
                }
                List<string> event_loc_id_list = new List<string>();
                foreach (XmlNode itemNode in itemNodeList)
                {
                    // use loc sequence
                    if ("loc" == itemNode.Name)
                    {
                        event_loc_id_list.Add(itemNode.InnerText);
                    }
                }

                // get action name of policy by action id (sql server)
                ArrayList list;
                Query("Policy", "Nm", "Id=" + int.Parse(action_id), out list);
                if (1 == list.Count)
                {
                    strPolicyName = ((string[])list[0])[0];
                }

                // get event id by event loc id (customized mapping)
                List<int> listEventIds = new List<int>();
                listEventIds.Clear();
                int event_id = -1;
                foreach (string loc_id in event_loc_id_list)
                {
                    event_id = GetEventTypeId(loc_id);
                    if (-1 == event_id) // invalid id
                    {
                        continue;
                    }
                    // append it if not included
                    if (!listEventIds.Contains(event_id))
                    {
                        listEventIds.Add(event_id);
                    }
                }

                // filter action events  (events configuration)
                if (0 == listEventIds.Count)
                {
                    return "No event needs to be taken action!";
                }
                mapActionEvents = new Dictionary<int, ArrayList>();
                mapActionEvents.Clear();
                EventCollection event_list = DeserializeFromXml<EventCollection>(System.Windows.Forms.Application.StartupPath.ToString() + @"\Events.conf");
                foreach (Event et in event_list.EventList)
                {
                    event_id = int.Parse(et.event_id);
                    if (listEventIds.Contains(event_id))
                    {
                        ArrayList action_list = et.actions;
                        mapActionEvents.Add(event_id, action_list);
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                strPolicyName = string.Empty;
                mapActionEvents = null;
                return "Failed to connect to avms database : " + ex.Message;
            }
        }

        private string MappingActionEvents(out string strPolicyName, out Dictionary<int, ArrayList> mapActionEvents)
        {
            try
            {
                strPolicyName = string.Empty;
                mapActionEvents = null;

                // filter action events  (events configuration)
                if (0 == m_listEventIds.Count)
                {
                    return "No event needs to be taken action!";
                }

                mapActionEvents = new Dictionary<int, ArrayList>();
                mapActionEvents.Clear();
                EventCollection event_list = DeserializeFromXml<EventCollection>(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + RULE_EVENT_CONFIG);
                if ((null == event_list.RuleList) || (0 == event_list.RuleList.Count()))
                {
                    return "No event is matched";
                }
                foreach (Event et in event_list.EventList)
                {
                    string event_id = et.event_id;
                    if (m_listEventIds.Contains(event_id))
                    {
                        strPolicyName += '[' + et.event_name + ']';
                        ArrayList action_list = et.actions;
                        mapActionEvents.Add(int.Parse(event_id), action_list);
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                strPolicyName = string.Empty;
                mapActionEvents = null;
                return "Failed to connect to avms database : " + ex.Message;
            }
        }

        #region AVMS Database

        private string Query(string table_name, string field, string condition, out ArrayList record_list)
        {
            return Query(table_name, new string[] { field }, new string[] { condition }, out record_list);
        }

        private string Query(string table_name, string[] fields, string[] conditions, out ArrayList record_list)
        {
            try
            {
                string statement = string.Empty;
                int count_fields = fields.Length;
                if (0 == count_fields)
                {
                    record_list = null;
                    return "Invalid Statement";
                }
                statement += "SELECT ";
                for (int i = 0; i < count_fields; i++)
                {
                    if (0 != i)
                    {
                        statement += ",";
                    }
                    statement += fields[i];
                }
                statement += " FROM " + table_name;

                int count_conditions = conditions.Length;
                for (int i = 0; i < count_conditions; i++)
                {
                    if (0 == i)
                    {
                        statement += " WHERE ";
                    }
                    else
                    {
                        statement += " AND ";
                    }
                    statement += conditions[i];
                }

                using (var myConn = VmsDatabase.CreateConnection())
                using (var cmd = myConn.CreateCommand(statement))
                {
                    record_list = new ArrayList();

                    cmd.CommandTimeout = 10000;
                    myConn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string[] record = new string[count_fields];
                            string val = string.Empty;
                            for (int i = 0; i < count_fields; i++)
                            {
                                val = reader[fields[i]].ToString();
                                record[i] = val;
                            }
                            record_list.Add(record);
                        }
                        reader.Close();
                    }
                    myConn.Close();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                record_list = null;
                return "Failed to connect to avms database : " + ex.Message;
            }
        }

        #endregion

        #region Events Configuration

        public static void SerializeToXml<T>(string filePath, T obj)
        {
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    xs.Serialize(writer, obj);
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                Trace.WriteLine(error);
            }
        }

        public static T DeserializeFromXml<T>(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    throw new ArgumentNullException(filePath + " not Exists");
                using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    T ret = (T)xs.Deserialize(reader);
                    return ret;
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                return default(T);
            }
        }

        //[XmlRoot("Configuration")]
        [XmlType(TypeName = "configuration")]
        public class EventCollection
        {
            [XmlArray("rule_event_map")]
            public Rule[] RuleList { get; set; }

            [XmlArray("events")]
            public Event[] EventList { get; set; }
        }

        [XmlType(TypeName = "rule")]
        public class Rule
        {
            // Attribute

            [XmlAttribute("id")]
            public string rule_id { get; set; }

            [XmlAttribute("name")]
            public string rule_name { get; set; }

            [XmlText]
            public string event_id { get; set; }    // separated by ','
        }

        //[XmlRoot("event")]  // XmlRootAttribute
        [XmlType(TypeName = "event")]
        public class Event
        {
            // Attribute

            [XmlAttribute("id")]
            public string event_id { get; set; }

            [XmlAttribute("name")]
            public string event_name { get; set; }

            [XmlArray("actions"), XmlArrayItem("action", typeof(Action))]
            public ArrayList actions = new ArrayList();
        }

        //[XmlRoot("action")]
        [XmlType(TypeName = "action")]
        public class Action
        {
            // Attribute

            [XmlAttribute("id")]    // [XmlIgnore] to ignore the id
            public string action_id { get; set; }

            [XmlAttribute("type")]
            public string action_type { get; set; }

            // Sub-Element

            // append detail for command
            [XmlElement("command", typeof(Command))]
            public List<Command> commands { get; set; }
        }

        // append detail for command
        [XmlType(TypeName = "command")]
        public class Command
        {
            [XmlAttribute("id")]
            public string command_id { get; set; }

            [XmlAttribute("desc")]
            public string command_desc { get; set; }

            [XmlElement("method")]
            public string command_method { get; set; }

            [XmlElement("url")]
            public string command_url { get; set; }

            [XmlElement("body")]
            public string command_body { get; set; }

            [XmlElement("body_elements", typeof(BodyElement))]
            public BodyElement body_elements { get; set; }
        }

        [XmlType(TypeName = "body_elements")]
        public class BodyElement
        {
            [XmlElement("execute_type")]
            public string execute_type { get; set; }

            [XmlElement("door_id")]
            public string door_id { get; set; }
        }

        #endregion

        #region Json

        // check and format json string
        private string ConvertJsonString(string str)
        {
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                Trace.WriteLine("Invalid Json format");
                return string.Empty;
            }
        }

        #endregion
    }
}
