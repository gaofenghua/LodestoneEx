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

namespace TransactionServer.Jobs.Client_Com
{
    public struct Client_Info
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
    }
 
    public class CC_SocketServer
    {
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
        }

        private void Server_OnAccept(int obj)
        {
            //server.SetAttached(obj, 555);
            Console.WriteLine($"Pack已连接{obj}");

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
            //int aaa = server.GetAttached<int>(arg1);
            //Console.WriteLine($"Pack已接收:{arg1} 长度:{arg2.Length}");          
            //server.Send(arg1, arg2, 0, arg2.Length);

            Parse_Received_Data(arg1, arg2);
        }

        private void Server_OnClose(int obj)
        {
            //int aaa = server.GetAttached<int>(obj);
            Console.WriteLine($"Pack断开{obj}");
        }

        private void Server_OnDisconnect(int obj)
        {
            //int aaa = server.GetAttached<int>(obj);
            Console.WriteLine($"Pack中断{obj}");
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

            for(int i=0;i<5;i++)
            {
                CameraList[i].ID = i;
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
    }
}
