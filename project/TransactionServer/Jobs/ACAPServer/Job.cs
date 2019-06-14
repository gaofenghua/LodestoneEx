using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.ServiceModel;
using Newtonsoft.Json;
using TransactionServer.Base;


namespace TransactionServer.Jobs.ACAPServer
{
    public class Job : Base.ServiceJob
    {
        private string m_jobName = string.Empty;
        private string m_serverIp = string.Empty;
        private string m_serverPort = string.Empty;
        private string m_serverUsername = string.Empty;
        private string m_serverPassword = string.Empty;
        private string m_workDirectory = string.Empty;
        private bool m_bTraceLogEnabled = true;
        private bool m_bPrintLogEnabled = false;
        private ServiceHost m_serviceHost;
        private GAT1400Service m_service = null;

        private const string OWNER = "ACAPServer";
        private const string IP_ADDRESS = "127.0.0.1";
        private const string PORT = "7788";
        private const string USERNAME = "admin";
        private const string PASSWORD = "admin";
        private const string CONFIG_FILE = "transaction.conf";
        private const string JOB_LOG_FILE = "TransactionServer.log";


        private void PrintLog(string text)
        {
            if (m_bTraceLogEnabled)
            {
                Trace.WriteLine(text);
            }
            if (m_bPrintLogEnabled)
            {
                ServiceTools.WriteLog(m_workDirectory + @"\" + JOB_LOG_FILE, text, true);
            }
        }

        private void ACAPService_DataUpdatedEvent(object sender, EventArgs e)
        {
            APEArgs arg = e as APEArgs;
            if ((null != arg) && (null != arg.Info))
            {
                APEInfo info = arg.Info;
                ACAPCamera cam = new ACAPCamera();
                cam.SetCameraIp(info.m_deviceIp);
                cam.SetACAPType(info.m_acapType);
            }
        }

        protected override void Init()
        {
            m_bPrintLogEnabled = (ServiceTools.GetAppSetting("print_log_enabled").ToLower() == "true") ? true : false;
            Config config = this.ConfigObject as Config;
            m_jobName = config.Description;
            m_serverIp = (config.Ip == string.Empty) ? IP_ADDRESS : config.Ip;
            m_serverPort = (config.Port == string.Empty) ? PORT : config.Port;
            if ((string.Empty != config.AuthInfo) && (2 == config.AuthInfo.Split(':').Length))
            {
                m_serverUsername = config.AuthInfo.Split(':')[0];
                m_serverPassword = config.AuthInfo.Split(':')[1];
            }
            else
            {
                m_serverUsername = USERNAME;
                m_serverPassword = PASSWORD;
            }
            m_workDirectory = System.Windows.Forms.Application.StartupPath.ToString();
        }

        protected override void Cleanup()
        {
            m_jobName = string.Empty;
            m_serverIp = string.Empty;
            m_serverPort = string.Empty;
            m_serverUsername = string.Empty;
            m_serverPassword = string.Empty;

        }

        protected override void Start()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                this.m_IsRunning = true;

                m_service = new GAT1400Service();
                m_service.APEInfoUpdateEvent += new EventHandler<EventArgs>(this.ACAPService_DataUpdatedEvent);
                string url = string.Format("http://{0}:{1}", m_serverIp, m_serverPort);
                Uri baseAddress = new Uri("http://127.0.0.1:7788/");
                using (m_serviceHost = new ServiceHost(m_service, baseAddress))
                {
                    WebHttpBinding binding = new WebHttpBinding
                    {
                        TransferMode = TransferMode.Buffered,
                        MaxBufferSize = 2147483647,
                        MaxReceivedMessageSize = 2147483647,
                        MaxBufferPoolSize = 2147483647,
                        ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                        Security = { Mode = WebHttpSecurityMode.None }
                    };
                    m_serviceHost.AddServiceEndpoint(typeof(IServiceCom), binding, baseAddress);
                    m_serviceHost.Opened += delegate
                    {
                        PrintLog("GAT1400Service has been opened");
                    };
                    m_serviceHost.Open();
                }

                PrintLog(String.Format("{0} - {1} : successful", m_jobName, methodName));
            }
            catch (Exception error)
            {
                this.m_IsRunning = false;
                PrintLog(String.Format("{0} - {1} : failed with exception \"{2}\"", m_jobName, methodName, error.ToString()));
                throw error;
            }
            finally
            {
                //
            }
        }

        protected override void Stop()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            PrintLog(String.Format("{0} - {1} : start", m_jobName, methodName));

            m_serviceHost.Close();
            PrintLog("GAT1400Service has been closed");
            m_service.APEInfoUpdateEvent -= new EventHandler<EventArgs>(this.ACAPService_DataUpdatedEvent);
            m_service = null;

            this.m_IsRunning = false;
            PrintLog(String.Format("{0} - {1} : end", m_jobName, methodName));
        }

        protected override void Callback_JobEventSend(object sender, JobEventArgs e)
        {
            //
        }

    }
}
