using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using TransactionServer.Base;
using socket.framework.Client;
using TC4I;
using System.Threading;

namespace TransactionServer.Jobs.Peake_Access
{
    enum Peake_Event
    {
        Controller_Damage = 0, Controller_Fire,
        OpenDoor_byButton, Illegal_Open, Close_Timeout,Reader_Demolish,
        Invalid_Card, Threated, Open_Success, End
    }
    class Peake_Access : Base.ServiceJob
    {
        public static string[] Event_Name =
        {
            "控制器被撬","控制器火警",
            "按钮开门","非法打开","超时未关","读卡器被拆",
            "无效刷卡","胁迫开门","开门成功"
        };

        private static bool m_bPrintLogAllowed = true;
        private const string JOB_LOG_FILE = "TransactionServer_Peake_Access.log";
        public static ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();

        public event Action<object, JobEventArgs> OnAlarm;

        public PA_Socket[] sockets;
        public PA_xmlConfig config;

        public static int Door_Number = 8;

        public static int socket_count = 0;

        private int Maximum_Controller_Number = 30;
        System.Threading.Timer heartbeat_timer = null;
        int Time_Interval = 60000*5;
        protected override void Init()
        {
            // throw new NotImplementedException();
           
        }

        protected override void Cleanup()
        {
            //throw new NotImplementedException();
        }

        protected override void Start()
        {
            try
            {
                this.m_IsRunning = true;
                this.executeLogic();
            }
            catch (Exception error)
            {
                //
            }
            finally
            {
                //
            }
        }

        protected override void Stop()
        {
            //client.Close();

            this.m_IsRunning = false;
        }

        protected override void Callback_JobEventSend(object sender, JobEventArgs e)
        {
            //
        }

        public void executeLogic()
        {
            Peake_Access.PrintLog(0, String.Format("++++++++++++ Peake_Access Started +++++++++++++++"));

            config = new PA_xmlConfig();
            config.Load_Event_Map();
            //config.Load_Systems();
            if (config.status == false)
            {
                Peake_Access.PrintLog(0, String.Format("error: configuration load failed, {0} Exit Peake_Access process.", config.message));
                return;
            }
            else if (config.message != "")
            {
                Peake_Access.PrintLog(0, String.Format("{0}", config.message));
            }

            //for (int i = 0; i < 5; i++)
            //{
            //    if (Global.Avms.IsConnected == false)
            //    {
            //        Peake_Access.PrintLog(0, String.Format("AVMS server is not connected. Wait 3 seconds and will try again"));
            //        System.Threading.Thread.Sleep(3000);
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            //if (Global.Avms.IsConnected == false)
            //{
            //    Peake_Access.PrintLog(0, String.Format("error: AVMS server is not connected. Exit Peake_Access process."));
            //    return;
            //}

            //Peake_Access.PrintLog(0, String.Format("AVMS server connected. Straring Peake_Access process."));


            int n_controller = config.Controllers.Count;

            if(n_controller > Maximum_Controller_Number)
            {
                Peake_Access.PrintLog(0, String.Format("error: The number of Controllers [{0}] exceed the capacity [{1}]. Exit Peake_Access process.",n_controller,Maximum_Controller_Number));
                return;
            }

            sockets = new PA_Socket[n_controller];

            for(int i=0;i<n_controller;i++)
            {
                PA_Controller con = config.Controllers[i];
                sockets[i] = new PA_Socket(con);
                sockets[i].parent = this;
                //sockets[i].OnAlarm += Peake_Access_OnAlarm;
                sockets[i].OnAlarm += this.OnJobEventSend;

                Thread.Sleep(200);
                //byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
                //sockets[i].Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);
                
                //// Check rules
                //sockets[i].Print_Rules();
            }



            //byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
            //byte[] Peak_Package_CMD_Upload = { 0x7e, 0xd0, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x18, 0x87 };
            //byte[] Peak_Package_CMD_OpenDoor = { 0x7e, 0x20, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x01, 0x03, 0x37, 0x03 };

            //client.Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);
            //// client.Send(Peak_Package_CMD_Upload, 0, Peak_Package_CMD_Upload.Length);
            ////client.Send(Peak_Package_CMD_OpenDoor, 0, Peak_Package_CMD_OpenDoor.Length);

            heartbeat_timer = new Timer(HeartBeat, null, Time_Interval, Timeout.Infinite);

        }

        private void Peake_Access_OnAlarm(object arg1, JobEventArgs arg2)
        {
            if(OnAlarm != null)
            {
                OnAlarm(arg1, arg2);
            }
        }

        public void HeartBeat(object obj)
        {
            int n_conn = sockets.Count();

            int n_Normal = 0;
            int n_Closed = 0;
            int n_Connecting = 0;

            string s_Normal = "id=";
            string s_Closed = "id=";
            string s_Connecting = "id=";

            for(int i=0;i<n_conn;i++)
            {
                if(sockets[i].status == Socket_Status.Normal)
                {
                    n_Normal += 1;
                    s_Normal = s_Normal + " " + sockets[i].id.ToString();
                }
                else if(sockets[i].status == Socket_Status.Closed)
                {
                    n_Closed += 1;
                    s_Closed = s_Closed + " " + sockets[i].id.ToString();
                }
                else if (sockets[i].status == Socket_Status.Connecting)
                {
                    n_Connecting += 1;
                    s_Connecting = s_Connecting + " " + sockets[i].id.ToString();
                }
            }

            Peake_Access.PrintLog(0, String.Format("PA_Heartbeat.Total controller = {0}. Connected={1} {2}. Closed={3} {4}. Connecting={5} {6}", n_conn,n_Normal,s_Normal,n_Closed,s_Closed,n_Connecting,s_Connecting));

            heartbeat_timer.Change(Time_Interval, Timeout.Infinite);
        }
        public static void PrintLog(int index, string text)
        {
            Trace.WriteLine(text);

            if (!m_bPrintLogAllowed)
            {
                return;
            }

            if(index == 2 )
            {
                return;
            }

            text = "Log=" + index.ToString() + " " + text;
            ServiceTools.WriteLog(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + JOB_LOG_FILE, text, true);
        }

  


    }

    enum Socket_Status { Init, Connecting, Normal, Connect_Failed, Closed };
    class PA_Socket
    {
        public event Action<object, JobEventArgs> OnAlarm;

        System.Threading.Timer heartbeat_timer = null;
        int Time_Interval = 1000;
        
        int heartbeat = 0;

        DateTime First_Reconnect_time = DateTime.MinValue;
        int Max_Reconnect_times = 5;
        TimeSpan Min_TimeSpan = TimeSpan.FromMinutes(5);
        int Reconnect_Times = 0;

        public Peake_Access parent;
        public TcpPushClient client;

        public int id;
        public string ip_add;
        public int port_num;
        AVMS_Policy_Rule[,] rules;
        
        public Socket_Status status;

        public void Print_Rules()
        {
            if(id != 1)
            {
                //return;
            }

            Peake_Access.PrintLog(0, String.Format("\r\n\r\nPA_Socket: id={0}, ip={1}",id,ip_add));

            for (int i = 0; i < Peake_Access.Door_Number+1; i++)
            {
                for (int j = 0; j < (int)Peake_Event.End; j++)
                {
                    Peake_Access.PrintLog(0, String.Format("rules[{0},{1}] policy={2}, camera={3} door={0}, event={4}, event_name={5}. ",i,j,rules[i,j].Policy_ID,rules[i,j].Camera_ID,j,Peake_Access.Event_Name[j]));
                }
            }
        }
        public PA_Socket(PA_Controller con)
        {
            id = con.ID;
            ip_add = con.IP;
            port_num = con.Port;

            First_Reconnect_time = DateTime.MinValue;

            rules = new AVMS_Policy_Rule[Peake_Access.Door_Number+1,(int)Peake_Event.End];

            for(int i=0;i< Peake_Access.Door_Number+1; i++)
            {
                for(int j=0;j< (int)Peake_Event.End;j++)
                {
                    rules[i,j] = con.Rules[i,j];
                }
            }

            client = new TcpPushClient(1024);
            client.OnConnect += Client_OnConnect;
            client.OnReceive += Client_OnReceive;
            client.OnSend += Client_OnSend;
            client.OnClose += Client_OnClose;
            client.OnDisconnect += Client_OnDisconnect;

            client.Connect(ip_add, port_num);

            status = Socket_Status.Connecting;

  
        }
        public PA_Socket(int receiveBufferSize, string ip, int port)
        {
            client = new TcpPushClient(receiveBufferSize);
            client.OnConnect += Client_OnConnect;
            client.OnReceive += Client_OnReceive;
            client.OnSend += Client_OnSend;
            client.OnClose += Client_OnClose;
            client.OnDisconnect += Client_OnDisconnect;

            client.Connect(ip, port);

            Peake_Access.socket_count += 1;

            status = Socket_Status.Connecting;
            ip_add = ip;
            port_num = port;
        }
        private void Client_OnClose()
        {
            Console.WriteLine($"pack断开");

            Peake_Access.PrintLog(0, String.Format("Peake_Access OnClosed. id={0}, ip={1}",id,ip_add));
        }
        private void Client_OnDisconnect()
        {
            Console.WriteLine($"pack中断");

            Peake_Access.PrintLog(0, String.Format("Peake_Access OnDisconnect. id={0}, ip={1}", id, ip_add));
        }

        public void Client_OnReceive(byte[] obj)
        {
            string rev = BitConverter.ToString(obj);

            // Treat as heartbeat
            if(is_HeartBeat(obj) == true)
            {
                return;
            }

            Peake_Access.PrintLog(0, String.Format("Peake_Access {1} id={2} Received [{0}]", rev, ip_add, id));

            int i = 0;
            while (i < obj.Length)
            {
                if (obj[i] == 0x7e)
                {
                    int cmd_data_len = obj[i+7] * 16 + obj[i+8];
                    int package_len = cmd_data_len + 11;

                    int data_begin = i + 9;
                    int data_len = cmd_data_len;

                    switch (obj[i+1])
                    {
                        case 0x1a: //按钮开门，门磁报警数据上传
                            ParseData_0x1A(data_begin, data_len, obj);
                            break;
                        case 0x1c: //防撬/火警报警数据上传
                            ParseData_0x1C(data_begin, data_len, obj);
                            break;
                        case 0xd0:  //启用主动上传模式（0xD0）
                            Peake_Access.PrintLog(0, String.Format("Peake_Access received package: 启用主动上传模式  [{0}]", BitConverter.ToString(obj,i,package_len)));
                            break;
                        case 0xd1: //上传全部报警数据（0xD1）
                            Peake_Access.PrintLog(0, String.Format("Peake_Access received package: 上传全部报警数据（0xD1）  [{0}]", BitConverter.ToString(obj, i, package_len)));
                            break;
                        case 0x1e: //刷卡/密码开门数据上传 （0x1E)

                            //int data_num = obj[data_begin];
                            //for(int n =0;n<data_num;n++)
                            //{
                            //    //log = String.Format("卡号 [{0}], 门号[{1}], 开门结果[{2}]", BitConverter.ToString(obj, data_begin+1+n*12, 4), BitConverter.ToString(obj, data_begin + 5 + n * 12, 1), BitConverter.ToString(obj, data_begin + 6 + n * 12, 1));
                            //    //Trace.WriteLine(log);
                            //    //Peake_Access.PrintLog(log);


                            //    string CardNumber = BitConverter.ToString(obj, data_begin + 1 + n * 12, 4);
                            //    byte b_Doornum = obj[data_begin + 5 + n * 12];
                            //    byte OpenDoor_Result = obj[data_begin + 6 + n * 12];

                            //    byte Mask_DoorNumber = 0x01;
                            //    int DoorNumber = 1;
                            //    for(;DoorNumber<9;DoorNumber++)
                            //    {
                            //        if((b_Doornum & Mask_DoorNumber) == Mask_DoorNumber)
                            //        {
                            //            break;
                            //        }
                            //        b_Doornum = (byte)(b_Doornum >> 1);
                            //    }


                            //    byte Mask_ValidCard = 0x80;
                            //    if ((OpenDoor_Result & Mask_ValidCard) != Mask_ValidCard)
                            //    {
                            //        int policy_id = rules[DoorNumber,(int)Peake_Event.Invalid_Card].Policy_ID;
                            //        int camera_id = rules[DoorNumber,(int)Peake_Event.Invalid_Card].Camera_ID;

                            //        log = String.Format("报警： 无效刷卡 卡号[{0}], 门号[{1}], CameraID={2}, PolicyID={3}.", CardNumber, DoorNumber, camera_id, policy_id);
                            //        Trace.WriteLine(log);
                            //        Peake_Access.PrintLog(log);

                            //        bool ret = Global.Avms.TriggerAlarm(camera_id, policy_id);
                            //        if (ret == false)
                            //        {
                            //            log = String.Format("error: Trigger Alarm Failed. {0}", Global.Avms.message);
                            //            Trace.WriteLine(log);
                            //            Peake_Access.PrintLog(log);
                            //        }
                            //    }
                            // }
                            ParseData_0x1E(data_begin, data_len, obj);
                            break;
                    }

                    i = i + package_len;
                }
                else
                {
                    break;
                }
            }
           
        }

        private void Client_OnConnect(bool obj)
        {
            //Console.WriteLine($"pack连接{obj}");

            if (obj == false)
            {
                status = Socket_Status.Connect_Failed;

                Peake_Access.PrintLog(0, String.Format("error: Peake_Access Connect failed. id={2}, ip={0}, port={1}. Close.", ip_add, port_num, id));
            }
            else
            {
                status = Socket_Status.Normal;

                Peake_Access.PrintLog(0, String.Format("Peake_Access Connected. id={2}, ip={0}, port={1}. ", ip_add, port_num, id));
            }

            heartbeat = 0;
            if (heartbeat_timer == null)
            {
                heartbeat_timer = new Timer(HeartBeat, null, Time_Interval, Timeout.Infinite);
            }
            else
            {
                heartbeat_timer.Change(Time_Interval, Timeout.Infinite);
            }

        }

        public void Send(byte[] data, int offset, int length)
        {
            client.Send(data, offset, length);

        }

        private void Client_OnSend(int obj)
        {
            Console.WriteLine($"pack已发送长度{obj}");

            //if(id == 20)
            //{
            //    string log = String.Format("Debugging: Socket OnSend, client.connected = {0}, id={1}, obj={2}.", client.Connected, id, obj);
            //    Trace.WriteLine(log);
            //    Peake_Access.PrintLog(log);
            //}
        }

        public void Close()
        {
            client.Close();
        }

        public bool Check_Reconnect_TooMany()
        {
            if (First_Reconnect_time == DateTime.MinValue)
            {
                First_Reconnect_time = DateTime.Now;
            }

            if (Reconnect_Times > Max_Reconnect_times)
            {
                TimeSpan timespan = DateTime.Now - First_Reconnect_time;
                if (timespan < Min_TimeSpan || Reconnect_Times > Max_Reconnect_times * 3)
                {
                    return true;
                }
            }
            return false;
        }
        public void ReConnect()
        {
            if (status == Socket_Status.Connecting)
            {
                return;
            }

            if(Check_Reconnect_TooMany() == true)
            {
                Peake_Access.PrintLog(0, String.Format("error: Socket reconnect too frequently, Force close. id={0}. {1} - {2} reconnect {3} times", id, First_Reconnect_time, DateTime.Now, Reconnect_Times));
         
                Close();
                status = Socket_Status.Closed;
                return;
            }

            Reconnect_Times = Reconnect_Times + 1;
            status = Socket_Status.Connecting;

            Peake_Access.PrintLog(0, String.Format("Warning: Socket ReConnecting, client.connected = {0}, id={1}.", client.Connected, id));

            Peake_Access.socket_count += 1;

            client.Close();
            Thread.Sleep(500);
            client.Reconnect(ip_add, port_num);

        }

        public void HeartBeat(object obj)
        {
            if (heartbeat < -5)
            {
                if (status != Socket_Status.Connecting)
                {
                    Peake_Access.PrintLog(0, String.Format("Warning: Socket Heartbeat failed. Start Reconnect, client.connected = {0}, id={1}.", client.Connected, id));
                    ReConnect();
                    heartbeat = 0;
                    return;
                }
            }

            if (client.Connected == true)
            {
                byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
                Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);

                if (id == 20)
                {
                    Peake_Access.PrintLog(2, String.Format("Debugging: HeartBeat sent, client.connected = {0}, status={2}, id={1}.", client.Connected, id, status));
                }
            }
            heartbeat = heartbeat - 1;
            heartbeat_timer.Change(Time_Interval, Timeout.Infinite);
        }

        public bool is_HeartBeat(byte[] data)
        {
            if (data.Length == 4 && data[0] == 0xaa && data[1] == 0xaa && data[2] == 0x00 && data[3] == 0xbb)
            {
                if (id == 20)
                {
                    Peake_Access.PrintLog(2, String.Format("Debugging: HeartBeat received, client.connected = {0}, id={1}.", client.Connected, id));
                }

                heartbeat = 0;
                return true;
            }
            return false;
        }

        public void ParseData_0x1A(int n_Begin, int n_Length, byte[] data)
        {
            byte door1234 = data[n_Begin];
            byte door5678 = data[n_Begin + 1]; //只有4门控制器，8门控制器先不做

            byte door_mask = 0x03;
            byte button_mask = 0x01;
            byte magnetic_mask = 0x02;

            int date_offset = 4;
            DateTime eventTime = ParseEventTime(n_Begin, date_offset, data);

            for (int door_num=1;door_num<5;door_num++)
            {
                if( (door1234 & door_mask & button_mask) == button_mask )
                {
                    //按钮开门
                    TriggerAlarm(door_num, Peake_Event.OpenDoor_byButton,eventTime);
                }

                if( (door1234 & door_mask & magnetic_mask) == magnetic_mask )
                {
                    //门磁报警
                    TriggerAlarm(door_num, Peake_Event.Illegal_Open,eventTime);
                }

                door1234 = (byte)(door1234 >> 2);
            }
        }

        public void ParseData_0x1C(int n_Begin, int n_Length, byte[] data)
        {
            byte status = data[n_Begin];

            byte fire_alarm_mask = 0x04;

            int date_offset = 4;
            DateTime eventTime = ParseEventTime(n_Begin, date_offset, data);

            if (0!= (status & fire_alarm_mask))
            {
                TriggerAlarm(0, Peake_Event.Controller_Fire,eventTime);
            }
            else
            {
                TriggerAlarm(0, Peake_Event.Controller_Damage,eventTime);
            }

        }
        public void ParseData_0x1E(int n_Begin, int n_Length, byte[] data)
        {
            int data_num = data[n_Begin];
            for (int n = 0; n < data_num; n++)
            {
                int date_offset = 1 + n*12 + 6;
                DateTime eventTime = ParseEventTime(n_Begin, date_offset,data);

                string CardNumber = BitConverter.ToString(data, n_Begin + 1 + n * 12, 4);
                byte b_Doornum = data[n_Begin + 5 + n * 12];
                byte OpenDoor_Result = data[n_Begin + 6 + n * 12];

                byte Mask_DoorNumber = 0x01;
                int DoorNumber = 1;
                for (; DoorNumber < 9; DoorNumber++)
                {
                    if ((b_Doornum & Mask_DoorNumber) == Mask_DoorNumber)
                    {
                        break;
                    }
                    b_Doornum = (byte)(b_Doornum >> 1);
                }

                byte Mask_ValidCard = 0x80;
                if ((OpenDoor_Result & Mask_ValidCard) != Mask_ValidCard)
                {
                    TriggerAlarm(DoorNumber, Peake_Event.Invalid_Card,eventTime);
                }

                if( (OpenDoor_Result >> 1 & 0x3F) == 0x01 )
                {
                    TriggerAlarm(DoorNumber, Peake_Event.Threated,eventTime);
                }
            }
        }

        private DateTime ParseEventTime(int n_Begin, int date_offset, byte[] data)
        {
            int year = DateTime.Now.Year;
            int month = Convert.ToInt32(BitConverter.ToString(data, n_Begin + date_offset + 1, 1));
            int day = Convert.ToInt32(BitConverter.ToString(data, n_Begin + date_offset + 2, 1));
            int hour = Convert.ToInt32(BitConverter.ToString(data, n_Begin + date_offset + 3, 1));
            int minute = Convert.ToInt32(BitConverter.ToString(data, n_Begin + date_offset + 4, 1));
            int second = Convert.ToInt32(BitConverter.ToString(data, n_Begin + date_offset + 5, 1));
            DateTime eventTime = new DateTime(year, month, day, hour, minute, second);

            return eventTime;
        }
        public void TriggerAlarm(int door_num, Peake_Event e, DateTime event_time)
        {
            int PA_Event = (int)e;

            int policy_id = rules[door_num, PA_Event].Policy_ID;
            int camera_id = rules[door_num, PA_Event].Camera_ID;

            if (policy_id != -1 && camera_id != -1)
            {
                //bool ret = Global.Avms.TriggerAlarm(camera_id, policy_id);
                //if (ret == false)
                //{
                //    Peake_Access.PrintLog(0, String.Format("error: Trigger Alarm Failed. {0}", Global.Avms.message));
                //}
                Peake_Access.PrintLog(0, String.Format("报警：{0}, 控制器={1}, 门号={2}, CameraID={3}, PolicyID={4}.", Peake_Access.Event_Name[PA_Event], id, door_num, camera_id, policy_id));

                if(OnAlarm != null)
                {
                    //JobEventArgs args = new JobEventArgs(this, "");
                    //OnAlarm(this,args);
                    int nEventTime = (int)(event_time - new DateTime(1970, 1, 1)).TotalSeconds;
                    JobEventInfo info = new JobEventInfo(nEventTime);    // alarm time
                    info.camera_id = camera_id;
                    info.policy_id = policy_id;
                    JobEventArgs args = new JobEventArgs(this, "", info);
                    OnAlarm(parent, args);
                }
            }
        }
    }
}
