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

namespace TransactionServer.Jobs.Client_Com
{
    class Client_Com : Base.ServiceJob
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

            MessageBox.Show("Start send data");

            Socket_Data CameraData = new Socket_Data();
            CameraData.Data_Type = Socket_Data_Type.Camera_Data;
            //CameraData.Photo= TC4I_Common.ReadImageFile("d:\\Axis_Code\\Sample_Image\\face\\20181211133729-0.jpg"); //d:\Axis_Code\Sample_Image\第三方智能分析截图.png
            CameraData.Photo2 = TC4I_Common.ReadImageFile("d:\\Axis_Code\\Sample_Image\\第三方智能分析截图.png"); //

            byte[] CameraData_Package = null;
            TC4I_Socket.serializeObjToByte(CameraData, out CameraData_Package);

            string path = "d:\\Axis_Code\\Sample_Image\\face";
            var files = Directory.GetFiles(path, "*.jpg");

            for(int i=0;i<1;i++)
            {
                foreach (var file in files)
                {
                    CameraData.Photo = TC4I_Common.ReadImageFile(file);
                    TC4I_Socket.serializeObjToByte(CameraData, out CameraData_Package);
                    SocketServer.server.Send(1, CameraData_Package, 0, CameraData_Package.Length);
                }
            }
      
        }
    }
 
    public class CC_SocketServer
    {
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
            object deserializeData = null;
            TC4I_Socket.deserializeByteToObj(rev, out deserializeData);
            Socket_Data RevData = (Socket_Data)deserializeData;

            switch(RevData.Data_Type)
            {
                case Socket_Data_Type.Heartbeat:
                    server.Send(clientID, rev,0,rev.Length);
                    break;
            }
        }
    }
}
