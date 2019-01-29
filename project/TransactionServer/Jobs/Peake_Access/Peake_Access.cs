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
    class Peake_Access : Base.ServiceJob
    {
        private static bool m_bPrintLogAllowed = true;
        private const string JOB_LOG_FILE = "TransactionServer_Peake_Access.log";

        public PA_Socket client;
        public PA_xmlConfig config;

        System.Threading.Timer heartbeat_timer;
        int heartbeat = 0;

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
            client.Close();

            this.m_IsRunning = false;
        }

        public void executeLogic()
        {
            string log;
            log = String.Format("++++++++++++ Peake_Access Started +++++++++++++++");
            Trace.WriteLine(log);
            PrintLog(log);

            config = new PA_xmlConfig();
            config.Load_Config();
            if(config.status == false)
            {
                log = String.Format("error: configuration load failed, {0} Exit Peake_Access process.",config.message);
                Trace.WriteLine(log);
                PrintLog(log);
                return;
            }

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


          
            int port = 5768;
            string ip = "192.168.77.101";
            int receiveBufferSize = 1024;
   
            client = new PA_Socket(receiveBufferSize,ip,port);
            client.parent = this;

            byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
            byte[] Peak_Package_CMD_Upload = { 0x7e, 0xd0, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x18, 0x87 };
            byte[] Peak_Package_CMD_OpenDoor = { 0x7e, 0x20, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x01, 0x03, 0x37, 0x03 };

            client.Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);
            // client.Send(Peak_Package_CMD_Upload, 0, Peak_Package_CMD_Upload.Length);
            //client.Send(Peak_Package_CMD_OpenDoor, 0, Peak_Package_CMD_OpenDoor.Length);

            //heartbeat_timer = new System.Threading.Timer(HeartBeat, null, 1000, 3000);
            heartbeat_timer = new Timer(HeartBeat, null, 3000, Timeout.Infinite);

        }

        public static void PrintLog(string text)
        {
            if (!m_bPrintLogAllowed)
            {
                return;
            }
            ServiceTools.WriteLog(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + JOB_LOG_FILE, text, true);
        }

        public void HeartBeat(object obj)
        {
            byte[] Peak_Package_CMD_AllowDataUpload = { 0xaa, 0xaa, 0x03, 0x01, 0xbb }; //允许数据主动上传
            client.Send(Peak_Package_CMD_AllowDataUpload, 0, Peak_Package_CMD_AllowDataUpload.Length);

            heartbeat = heartbeat - 1;

            if(heartbeat < -5)
            {
                if(client.status !=  Socket_Status.Connecting)
                {
                    client.ReConnect();
                    heartbeat = 0;

                    string log = String.Format("error: Peake_Access heartbeat failed ({0}). Re-Connecting...", heartbeat);
                    Trace.WriteLine(log);
                    PrintLog(log);
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
    }

    enum Socket_Status { Init, Connecting, Normal };
    class PA_Socket
    {
        public Peake_Access parent;
        public TcpPushClient client;
        public string ip_add;
        public int port_num;
        
        public Socket_Status status;
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

            string log = String.Format("-------------- Peake_Access Closed -----------------");
            Trace.WriteLine(log);
            Peake_Access.PrintLog(log);
        }
        private void Client_OnDisconnect()
        {
            Console.WriteLine($"pack中断");
        }

        private void Client_OnReceive(byte[] obj)
        {
            string rev = BitConverter.ToString(obj);

            string log;
            log = String.Format("Peake_Access Received [{0}]",rev);
            Trace.WriteLine(log);
            Peake_Access.PrintLog(log);

            // Treat as heartbeat
            if(parent.is_HeartBeat(obj) == true)
            {
                return;
            }

            int i = 0;
            while (i < obj.Length)
            {
                if (obj[i] == 0x7e)
                {
                    int cmd_data_len = obj[i+7] * 16 + obj[i+8];
                    int package_len = cmd_data_len + 11;

                    switch(obj[i+1])
                    {
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
                            //log = String.Format("Peake_Access received package: 刷卡/密码开门数据上传 （0x1E)  [{0}]", BitConverter.ToString(obj, i, package_len));
                            //Trace.WriteLine(log);
                            //Peake_Access.PrintLog(log);

                            int data_begin = i + 9;
                            int data_len = obj[i + 8];

                            int data_num = obj[data_begin];
                            for(int n =0;n<data_num;n++)
                            {
                                //log = String.Format("卡号 [{0}], 门号[{1}], 开门结果[{2}]", BitConverter.ToString(obj, data_begin+1+n*12, 4), BitConverter.ToString(obj, data_begin + 5 + n * 12, 1), BitConverter.ToString(obj, data_begin + 6 + n * 12, 1));
                                //Trace.WriteLine(log);
                                //Peake_Access.PrintLog(log);

                               
                                string CardNumber = BitConverter.ToString(obj, data_begin + 1 + n * 12, 4);
                                byte b_Doornum = obj[data_begin + 5 + n * 12];
                                byte OpenDoor_Result = obj[data_begin + 6 + n * 12];

                                byte Mask_DoorNumber = 0x01;
                                int DoorNumber = 1;
                                for(;DoorNumber<9;DoorNumber++)
                                {
                                    if((b_Doornum & Mask_DoorNumber) == Mask_DoorNumber)
                                    {
                                        break;
                                    }
                                    b_Doornum = (byte)(b_Doornum >> 1);
                                }


                                byte Mask_ValidCard = 0x80;
                                if ((OpenDoor_Result & Mask_ValidCard) != Mask_ValidCard)
                                {
                                    int policy_id = parent.config.rules[(int)Peake_Event.Invalid].Policy_ID;
                                    int camera_id = parent.config.rules[(int)Peake_Event.Invalid].Camera_ID;

                                    log = String.Format("报警： 无效刷卡 卡号[{0}], 门号[{1}], CameraID={2}, PolicyID={3}.", CardNumber,DoorNumber,camera_id,policy_id);
                                    Trace.WriteLine(log);
                                    Peake_Access.PrintLog(log);

                                    bool ret = Global.Avms.TriggerAlarm(camera_id,policy_id);
                                    if (ret == false)
                                    {
                                        log = String.Format("error: Trigger Alarm Failed. {0}", Global.Avms.message);
                                        Trace.WriteLine(log);
                                        Peake_Access.PrintLog(log);
                                    }
                                }
                             }

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

            status = Socket_Status.Normal;
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
    }
}
