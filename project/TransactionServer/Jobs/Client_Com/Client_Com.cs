using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using socket.framework.Server;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;
using TC4I_Socket;

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
        }
    }
 
    public class CC_SocketServer
    {
        TcpPackServer server;
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
            server.Send(arg1, arg2, 0, arg2.Length);

            object deserializeData = null;
            deserializeByteToObj(arg2, out deserializeData);
            Socket_Data RevData = (Socket_Data)deserializeData;
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

        public bool serializeObjToStr(Object obj, out string serializedStr)
        {
            bool serializeOk = false;
            serializedStr = "";
            try
            {
                MemoryStream memoryStream = new MemoryStream();
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, obj);
                serializedStr = System.Convert.ToBase64String(memoryStream.ToArray());

                serializeOk = true;
            }
            catch
            {
                serializeOk = false;
            }

            return serializeOk;
        }

        public bool deserializeStrToObj(string serializedStr, out object deserializedObj)
        {
            bool deserializeOk = false;
            deserializedObj = null;

            try
            {
                byte[] restoredBytes = System.Convert.FromBase64String(serializedStr);
                MemoryStream restoredMemoryStream = new MemoryStream(restoredBytes);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                deserializedObj = binaryFormatter.Deserialize(restoredMemoryStream);

                deserializeOk = true;
            }
            catch
            {
                deserializeOk = false;
            }

            return deserializeOk;
        }

        public bool serializeObjToByte(Object obj, out byte[] serializedByte)
        {
            bool serializeOk = false;
            serializedByte = null;
            try
            {
                MemoryStream memoryStream = new MemoryStream();
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, obj);
                serializedByte = memoryStream.ToArray();

                serializeOk = true;
            }
            catch
            {
                serializeOk = false;
            }

            return serializeOk;
        }

        public bool deserializeByteToObj(Byte[] serializedByte, out object deserializedObj)
        {
            bool deserializeOk = false;
            deserializedObj = null;

            try
            {
                MemoryStream restoredMemoryStream = new MemoryStream(serializedByte);
                //BinaryFormatter binaryFormatter = new BinaryFormatter();
                IFormatter formatter = new BinaryFormatter();
                formatter.Binder = new UBinder();

                //deserializedObj = binaryFormatter.Deserialize(restoredMemoryStream);
                deserializedObj = formatter.Deserialize(restoredMemoryStream);
                deserializeOk = true;
            }
            catch
            {
                deserializeOk = false;
            }

            return deserializeOk;
        }

        public class UBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                Assembly ass = Assembly.GetExecutingAssembly();
                return ass.GetType(typeName);
            }
        }
    }
}
