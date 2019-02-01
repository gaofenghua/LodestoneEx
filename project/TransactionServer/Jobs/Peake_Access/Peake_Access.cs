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

        //public PA_Socket client;
        public PA_Socket[] sockets;
        public PA_xmlConfig config;

        public static int Door_Number = 8;
  
     
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

        public void executeLogic()
        {
            string log;
            log = String.Format("++++++++++++ Peake_Access Started +++++++++++++++");
            Trace.WriteLine(log);
            PrintLog(log);

            config = new PA_xmlConfig();
            config.Load_Systems();
            if (config.status == false)
            {
                log = String.Format("error: configuration load failed, {0} Exit Peake_Access process.", config.message);
                Trace.WriteLine(log);
                PrintLog(log);
                return;
            }
            else if (config.message != "")
            {
                log = String.Format("{0}", config.message);
                Trace.WriteLine(log);
                PrintLog(log);
            }

          

            //config = new PA_xmlConfig();
            //config.Load_Config();
            //if(config.status == false)
            //{
            //    log = String.Format("error: configuration load failed, {0} Exit Peake_Access process.",config.message);
            //    Trace.WriteLine(log);
            //    PrintLog(log);
            //    return;
            //}

            for (int i = 0; i < 5; i++)
            {
                if (Global.Avms.IsConnected == false)
                {
                    log = String.Format("AVMS server is not connected. Wait 3 seconds and will try again");
                    System.Threading.Thread.Sleep(3000);
                    Trace.WriteLine(log);
                    PrintLog(log);
                }
                else
                {
                    break;
                }
            }

            if (Global.Avms.IsConnected == false)
            {
                log = String.Format("error: AVMS server is not connected. Exit Peake_Access process.");
                Trace.WriteLine(log);
                PrintLog(log);
                return;
            }
          
            log = String.Format("AVMS server connected. Straring Peake_Access process.");
            Trace.WriteLine(log);
            PrintLog(log);


            int n_controller = config.Controllers.Count;
            sockets = new PA_Socket[n_controller];

            for(int i=0;i<n_controller;i++)
            {
                PA_Controller con = config.Controllers[i];
                sockets[i] = new PA_Socket(con);
                sockets[i].parent = this;

                byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
                sockets[i].Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);
                
                //// Check rules
                //sockets[i].Print_Rules();
            }

 

            //byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
            //byte[] Peak_Package_CMD_Upload = { 0x7e, 0xd0, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x18, 0x87 };
            //byte[] Peak_Package_CMD_OpenDoor = { 0x7e, 0x20, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x01, 0x03, 0x37, 0x03 };

            //client.Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);
            //// client.Send(Peak_Package_CMD_Upload, 0, Peak_Package_CMD_Upload.Length);
            ////client.Send(Peak_Package_CMD_OpenDoor, 0, Peak_Package_CMD_OpenDoor.Length);

     

        }

        public static void PrintLog(string text)
        {
            if (!m_bPrintLogAllowed)
            {
                return;
            }
            ServiceTools.WriteLog(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + JOB_LOG_FILE, text, true);
        }

  


    }

    enum Socket_Status { Init, Connecting, Normal, Connect_Failed };
    class PA_Socket
    {
        System.Threading.Timer heartbeat_timer;
        int heartbeat = 0;

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

            string log = String.Format("\r\n\r\nPA_Socket: id={0}, ip={1}",id,ip_add);
            Trace.WriteLine(log);
            Peake_Access.PrintLog(log);

            for (int i = 0; i < Peake_Access.Door_Number+1; i++)
            {
                for (int j = 0; j < (int)Peake_Event.End; j++)
                {
                    log = String.Format("rules[{0},{1}] policy={2}, camera={3} door={0}, event={4}, event_name={5}. ",i,j,rules[i,j].Policy_ID,rules[i,j].Camera_ID,j,Peake_Access.Event_Name[j]);
                    Trace.WriteLine(log);
                    Peake_Access.PrintLog(log);
                }
            }
        }
        public PA_Socket(PA_Controller con)
        {
            id = con.ID;
            ip_add = con.IP;
            port_num = con.Port;

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

            status = Socket_Status.Connecting;
            ip_add = ip;
            port_num = port;
        }
        private void Client_OnClose()
        {
            Console.WriteLine($"pack断开");

            string log = String.Format("Peake_Access Closed. id={0}, ip={1}",id,ip_add);
            Trace.WriteLine(log);
            Peake_Access.PrintLog(log);
        }
        private void Client_OnDisconnect()
        {
            Console.WriteLine($"pack中断");
        }

        public void Client_OnReceive(byte[] obj)
        {
            string rev = BitConverter.ToString(obj);

            // Treat as heartbeat
            if(is_HeartBeat(obj) == true)
            {
                return;
            }

            string log;
            log = String.Format("Peake_Access {1} id={2} Received [{0}]", rev, ip_add, id);
            Trace.WriteLine(log);
            Peake_Access.PrintLog(log);

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
                        case 0xd0:  //启用主动上传模式（0xD0）
                            log = String.Format("Peake_Access received package: 启用主动上传模式  [{0}]", BitConverter.ToString(obj,i,package_len));
                            Trace.WriteLine(log);
                            Peake_Access.PrintLog(log);
                            break;
                        case 0xd1: //上传全部报警数据（0xD1）
                            log = String.Format("Peake_Access received package: 上传全部报警数据（0xD1）  [{0}]", BitConverter.ToString(obj, i, package_len));
                            Trace.WriteLine(log);
                            Peake_Access.PrintLog(log);
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
            Console.WriteLine($"pack连接{obj}");

            if(obj == false)
            {
                status = Socket_Status.Connect_Failed;
                Close();

                string log = String.Format("error: Peake_Access Connect failed. id={2}, ip={0}, port={1}. Close.", ip_add,port_num,id);
                Trace.WriteLine(log);
                Peake_Access.PrintLog(log);
            }
            else
            {
                status = Socket_Status.Normal;
                heartbeat_timer = new Timer(HeartBeat, null, 3000, Timeout.Infinite);

                string log = String.Format("Peake_Access Connected. id={2}, ip={0}, port={1}. ", ip_add, port_num, id);
                Trace.WriteLine(log);
                Peake_Access.PrintLog(log);
            }
            
        }

        public void Send(byte[] data, int offset, int length)
        {
            client.Send(data, offset, length);

        }

        private void Client_OnSend(int obj)
        {
            Console.WriteLine($"pack已发送长度{obj}");
        }

        public void Close()
        {
            client.Close();
        }

        public void ReConnect()
        {
            if(status == Socket_Status.Connecting)
            {
                Console.WriteLine($"warning: Socket ReConnect, but the status is Connecting.");
                return;
            }
            client.Close();
            client.Connect(ip_add, port_num);
        }

        public void HeartBeat(object obj)
        {
            byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
            client.Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);

            heartbeat = heartbeat - 1;

            if (heartbeat < -5)
            {
                if (status != Socket_Status.Connecting)
                {
                    ReConnect();
                    heartbeat = 0;

                    string log = String.Format("error: Peake_Access heartbeat failed ({0}). Re-Connecting... id={1}, ip={2}, port={3}. ", heartbeat,id,ip_add,port_num);
                    Trace.WriteLine(log);
                    Peake_Access.PrintLog(log);
                    
                }

                //return;
            }

            heartbeat_timer.Change(3000, Timeout.Infinite);
        }

        public bool is_HeartBeat(byte[] data)
        {
            if (data.Length == 4 && data[0] == 0xaa && data[1] == 0xaa && data[2] == 0x00 && data[3] == 0xbb)
            {
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

            for(int door_num=1;door_num<5;door_num++)
            {
                if( (door1234 & door_mask & button_mask) == button_mask )
                {
                    //按钮开门
                    TriggerAlarm(door_num, Peake_Event.OpenDoor_byButton);
                }

                if( (door1234 & door_mask & magnetic_mask) == magnetic_mask )
                {
                    //门磁报警
                    TriggerAlarm(door_num, Peake_Event.Illegal_Open);
                }

                door1234 = (byte)(door1234 >> 2);
            }
        }

        public void ParseData_0x1E(int n_Begin, int n_Length, byte[] data)
        {
            int data_num = data[n_Begin];
            for (int n = 0; n < data_num; n++)
            {
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
                    TriggerAlarm(DoorNumber, Peake_Event.Invalid_Card);
                }

                if( (OpenDoor_Result >> 1 & 0x3F) == 0x01 )
                {
                    TriggerAlarm(DoorNumber, Peake_Event.Threated);
                }
            }

        }
        public void TriggerAlarm(int door_num, Peake_Event e)
        {
            string log;
            int PA_Event = (int)e;

            int policy_id = rules[door_num, PA_Event].Policy_ID;
            int camera_id = rules[door_num, PA_Event].Camera_ID;

            if (policy_id != -1 && camera_id != -1)
            {
                bool ret = Global.Avms.TriggerAlarm(camera_id, policy_id);
                if (ret == false)
                {
                    log = String.Format("error: Trigger Alarm Failed. {0}", Global.Avms.message);
                    Trace.WriteLine(log);
                    Peake_Access.PrintLog(log);
                }
                log = String.Format("报警：{0}, 控制器={1}, 门号={2}, CameraID={3}, PolicyID={4}.", Peake_Access.Event_Name[PA_Event], id, door_num, camera_id, policy_id);
                Trace.WriteLine(log);
                Peake_Access.PrintLog(log);
            }
        }
    }
}
