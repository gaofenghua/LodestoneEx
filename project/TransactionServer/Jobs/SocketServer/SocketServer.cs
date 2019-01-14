using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using socket.framework.Server;

namespace TransactionServer.Jobs.SocketServer
{
    class SocketServer : Base.ServiceJob
    {
        /* START: used for Messagebox in the windows service */
        public static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        public static void ShowMessageBox(string message, string title)
        {
            int resp = 0;
            WTSSendMessage(
                WTS_CURRENT_SERVER_HANDLE,
                WTSGetActiveConsoleSessionId(),
                title, title.Length,
                message, message.Length,
                0, 0, out resp, false);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSSendMessage(
            IntPtr hServer,
            int SessionId,
            String pTitle,
            int TitleLength,
            String pMessage,
            int MessageLength,
            int Style,
            int Timeout,
            out int pResponse,
            bool bWait);
        /* END: used for Messagebox in the windows service */

        TcpPackServer server;

        protected override void Init()
        {
            // throw new NotImplementedException();
            ShowMessageBox("This a message from SocketServer.", "Socket");
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

        private void executeLogic()
        {
            int port = 5555;
            int numConnections = 10;
            int receiveBufferSize = 1024;
            int overtime = 0;

            server = new TcpPackServer(numConnections, receiveBufferSize, overtime, 0xFF);
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

            string ipAdd = "ipadd = ";
            ipAdd = server.GetRemoteIPAddress(obj);
            ShowMessageBox(ipAdd, "Socket");
        }

        private void Server_OnSend(int arg1, int arg2)
        {
            //Console.WriteLine($"Pack已发送:{arg1} 长度:{arg2}");
        }

        private void Server_OnReceive(int arg1, byte[] arg2)
        {
            ShowMessageBox("Server OnReceive.", "Socket");
            //int aaa = server.GetAttached<int>(arg1);
            //Console.WriteLine($"Pack已接收:{arg1} 长度:{arg2.Length}");          
            server.Send(arg1, arg2, 0, arg2.Length);


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
    }
}
