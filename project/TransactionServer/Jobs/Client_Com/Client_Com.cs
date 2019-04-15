using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using socket.framework.Server;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TC4I;
using System.Windows;
using System.Collections.Concurrent;
using System.Threading;

namespace TransactionServer.Jobs.Client_Com
{
    public class Client_Info
    {
        public int ClientID;
        public int Heartbeat;
        public int RegisteredCameraID;
    }
    public class Client_Com : Base.ServiceJob
    {
        public CC_SocketServer SocketServer = null;
 
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
            this.m_IsRunning = false;
        }

        protected override void Callback_JobEventSend(object sender, JobEventArgs e)
        {
            //
        }

        public void executeLogic()
        {
            SocketServer = new CC_SocketServer(10, 1024, 0, 12345, 0xFF);
            SocketServer.parent = this;

            //MessageBox.Show("Start send data");
            return;

            Socket_Data SocketData = new Socket_Data();
            SocketData.DataType = Socket_Data_Type.Camera_Data;

            Camera_Data CameraData = new Camera_Data();
            CameraData.Photo= TC4I_Common.ReadImageFile("d:\\Axis_Code\\Sample_Image\\face\\20181211133729-0.jpg"); //d:\Axis_Code\Sample_Image\第三方智能分析截图.png
            CameraData.Photo2 = TC4I_Common.ReadImageFile("d:\\Axis_Code\\Sample_Image\\第三方智能分析截图.png"); //
            SocketData.SubData = CameraData;
            byte[] CameraData_Package = null;
            TC4I_Socket.serializeObjToByte(SocketData, out CameraData_Package);
            SocketServer.server.Send(1, CameraData_Package, 0, CameraData_Package.Length);

            string path = "d:\\Axis_Code\\Sample_Image\\face";
            var files = Directory.GetFiles(path, "*.jpg");

            for (int i = 0; i < 1; i++)
            {
                foreach (var file in files)
                {
                    CameraData.Photo = TC4I_Common.ReadImageFile(file);
                    SocketData.SubData = CameraData;
                    TC4I_Socket.serializeObjToByte(SocketData, out CameraData_Package);
                    SocketServer.server.Send(1, CameraData_Package, 0, CameraData_Package.Length);
                }
            }
        }
        public void OnACAPCameraListUpdate(object sender, EventArgs e)
        {
            DeviceArgs Arg = (DeviceArgs)e;

            int DeviceNumber = Arg.Cameras.Count();
            
            if(DeviceNumber <= 0)
            {
                return;
            }

            Camera_Info[] CameraList = new Camera_Info[DeviceNumber];

            for (int i = 0; i < DeviceNumber; i++)
            {
                CameraList[i].ID = Arg.Cameras[i].id;
                CameraList[i].Name = Arg.Cameras[i].name;
                CameraList[i].IP = Arg.Cameras[i].ip;
                CameraList[i].Status = Arg.Cameras[i].status;
                CameraList[i].Type = Arg.Cameras[i].type;
            }

            Command_Request CommandRequest = new Command_Request();
            CommandRequest.Command = Socket_Command.UpdateCameraList;
            CommandRequest.Arg = CameraList;

            SocketServer.SendToAll(CommandRequest, Socket_Data_Type.Command);
        }
    }
 
    public class CC_SocketServer
    {
        public static int Maximum_Connection_Number = 2;
        System.Threading.Timer Heartbeat_Timer = null;
        int Heartbeat_Time_Interval = 1000 * 5;

        public Client_Com parent;
        public ConcurrentDictionary<int, Client_Info> ClientList = new ConcurrentDictionary<int, Client_Info>();
        public TcpPackServer server;
        /// <summary>
        /// 设置基本配置
        /// </summary>   
        /// <param name="numConnections">同时处理的最大连接数</param>
        /// <param name="receiveBufferSize">用于每个套接字I/O操作的缓冲区大小(接收端)</param>
        /// <param name="overtime">超时时长,单位秒.(每10秒检查一次)，当值为0时，不设置超时</param>
        /// <param name="port">端口</param>
        /// <param name="headerFlag">包头</param>
        public CC_SocketServer(int numConnections, int receiveBufferSize, int overtime, int port, uint headerFlag)
        {
            server = new TcpPackServer(numConnections, receiveBufferSize, overtime, headerFlag);
            server.OnAccept += Server_OnAccept;
            server.OnReceive += Server_OnReceive;
            server.OnSend += Server_OnSend;
            server.OnClose += Server_OnClose;
            server.OnDisconnect += Server_OnDisconnect;
            server.Start(port);

            Heartbeat_Timer = new Timer(HeartBeat, null, Heartbeat_Time_Interval, Timeout.Infinite);
        }

        private void Server_OnAccept(int obj)
        {
            //server.SetAttached(obj, 555);
            Console.WriteLine($"Pack已连接{obj}");

            if(ClientList.Count()>= Maximum_Connection_Number)
            {
                string message = string.Format("Server connection over maximum number {0}/{1}, Connection closed. ",ClientList.Count(),Maximum_Connection_Number);
                Send(obj, message, Socket_Data_Type.Message);

                Command_Request CommandRequest = new Command_Request();
                CommandRequest.Command = Socket_Command.CloseSocket;
                CommandRequest.Arg = null;

                Send(obj, CommandRequest, Socket_Data_Type.Command);
                return;
            }

            Client_Info RemoteClient = new Client_Info();
            RemoteClient.ClientID = obj;
            RemoteClient.Heartbeat = 0;
            RemoteClient.RegisteredCameraID = -1;

            ClientList[obj] = RemoteClient;
        }

        private void Server_OnSend(int arg1, int arg2)
        {
            //Console.WriteLine($"Pack已发送:{arg1} 长度:{arg2}");
        }

        private void Server_OnReceive(int arg1, byte[] arg2)
        {
            Parse_Received_Data(arg1, arg2);
        }

        private void Server_OnClose(int obj)
        {
            //int aaa = server.GetAttached<int>(obj);
            Console.WriteLine($"Pack断开{obj}");
        }

        private void Server_OnDisconnect(int obj)
        {
            Client_Info ClientInfo;
            ClientList.TryRemove(obj, out ClientInfo);
        }

        public void Parse_Received_Data(int clientID, byte[] rev)
        {
            Client_Info RemoteClient;
            if(!ClientList.TryGetValue(clientID,out RemoteClient))
            {
                return;
            }

            object deserializeData = null;
            TC4I_Socket.deserializeByteToObj(rev, out deserializeData);
            Socket_Data RevData = (Socket_Data)deserializeData;

            RemoteClient.Heartbeat = 0;
            switch(RevData.DataType)
            {
                case Socket_Data_Type.Heartbeat:
                    Heartbeat_Data HeartbeatData = (Heartbeat_Data)RevData.SubData;
                    server.Send(clientID, rev,0,rev.Length);
                    break;
                case Socket_Data_Type.Command:
                    Command_Request CommandRequest = (Command_Request)RevData.SubData;
                    switch(CommandRequest.Command)
                    {
                        case Socket_Command.GetCameraList:
                            Command_GetCameraList(clientID);
                            break;
                    }
                    break;
            }
        }

        public void Command_GetCameraList(int ClientID)
        {
            Camera_Info[] CameraList = new Camera_Info[5];

            for (int i = 0; i < 5; i++)
            {
                CameraList[i].ID = (uint)i;
                CameraList[i].Name = string.Format("Camera_Name_{0}", i);
                CameraList[i].IP = string.Format("192.168.{0}.{0}", i);
                CameraList[i].Status = 0;
            }

            Command_Return CommandReturn = new Command_Return();
            CommandReturn.Command = Socket_Command.GetCameraList;
            CommandReturn.Result = CameraList;

            Send(ClientID, CommandReturn, Socket_Data_Type.Command_Return);
        }

        public void Send(int ClientID, object SendData, Socket_Data_Type DataType)
        {
            Socket_Data SocketData = new Socket_Data();
            SocketData.DataType = DataType;
            SocketData.SubData = SendData;

            byte[] SendPackage = null;
            TC4I_Socket.serializeObjToByte(SocketData, out SendPackage);
            server.Send(ClientID, SendPackage, 0, SendPackage.Length);
        }
        public void SendToAll(object SendData, Socket_Data_Type DataType)
        {
            Socket_Data SocketData = new Socket_Data();
            SocketData.DataType = DataType;
            SocketData.SubData = SendData;

            byte[] SendPackage = null;
            TC4I_Socket.serializeObjToByte(SocketData, out SendPackage);

            foreach(int ClientID in ClientList.Keys)
            {
                server.Send(ClientID, SendPackage, 0, SendPackage.Length);
            }
        }
        public void HeartBeat(object obj)
        {
            string message = string.Format("Server Heartbeat: Total Client = {0} ", ClientList.Count());

            List<int> ClientToRemove = new List<int>();
            //foreach(KeyValuePair<int, Client_Info> kvp in ClientList)
            //{
            //    message = message + string.Format("\r\nID={0}, Heartbeat={1} ",kvp.Value.ClientID,kvp.Value.Heartbeat);

            //    if(kvp.Value.Heartbeat < -3)
            //    {
            //        ClientToRemove.Add(kvp.Key);
            //    }
            //    else
            //    {
            //        Client_Info ClientInfo = ClientList[kvp.Key];
            //        ClientInfo.Heartbeat = ClientInfo.Heartbeat - 1;
            //    }
            //}
            foreach (Client_Info ClientInfo in ClientList.Values)
            {
                message = message + string.Format("\r\nID={0}, Heartbeat={1} ", ClientInfo.ClientID, ClientInfo.Heartbeat);

                if (ClientInfo.Heartbeat < -3)
                {
                    ClientToRemove.Add(ClientInfo.ClientID);
                }
                else
                {
                    ClientInfo.Heartbeat = ClientInfo.Heartbeat - 1;
                    //Client_Info newClient;
                    //ClientList.TryGetValue(ClientInfo.ClientID,out newClient);
                    //newClient.Heartbeat = ClientInfo.Heartbeat - 1;
                    //ClientList.TryUpdate(ClientInfo.ClientID, newClient,ClientInfo);
                }
            }
            foreach (int ClientID in ClientToRemove)
            {
                Client_Info ClientInfo;
                ClientList.TryRemove(ClientID,out ClientInfo);
                server.Close(ClientID);
            }

            TC4I_Common.PrintLog(0, message);
            Heartbeat_Timer.Change(Heartbeat_Time_Interval, Timeout.Infinite);
        }
    }
}
