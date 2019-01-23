using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace TransactionServerModules
{
    public class ServiceJob
    {
        public int m_jobId;
        public string m_jobName;
        //public AVMSCom m_avms = null;
        public AVMSCom m_avms { get; private set; } = null;
        public bool m_bConnectedToAVMSServer = false;
        public bool m_bDeviceModelEventHandlerAdded = false;

        public delegate void JobEventHandler(object sender, JobEventArgs e);    // agent
        public event JobEventHandler JobEventSend;  // event

        public event EventHandler<EventArgs> FarmConnectedEvent;

        public void InitAVMSServer(string Ip, string Username, string Password)
        {
            m_avms = new AVMSCom(Ip, Username, Password);
            m_avms.MessageSend += new AVMSCom.MessageEventHandler(this.AVMSCom_MessageSend);
        }

        public void OnJobEventSend(object sender, JobEventArgs e)
        {
            if (null != JobEventSend)
            {
                this.JobEventSend(sender, e);
            }
        }

        public void OnFarmConnected(object sender, EventArgs e)
        {
            if (null != FarmConnectedEvent)
            {
                this.FarmConnectedEvent(sender, e);
            }
        }

        private void AVMSCom_MessageSend(object sender, MessageEventArgs e)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            string message = e.Message;
            if ((string.Empty == message) || (2 != message.Split('\t').Length))
            {
                Trace.WriteLine(String.Format("Job_{0}[{1}] : not invalid message", m_jobId, methodName));
                return;
            }

            string time = message.Split('\t')[0];
            switch (message.Split('\t')[1])
            {
                case "Connect":

                    m_bConnectedToAVMSServer = m_avms.IsConnected;
                    if (m_bConnectedToAVMSServer)
                    {
                        message = String.Format("Job_{0}[{1}] : [{2}]connection has been established", m_jobId, methodName, time);
                        Trace.WriteLine(message);
                        //this.OnJobEventSend(this, new JobEventArgs(this, message));
                        this.OnFarmConnected(this, new EventArgs());
                    }

                    break;

                case "Disconnect":

                    m_bConnectedToAVMSServer = m_avms.IsConnected;
                    if (!m_bConnectedToAVMSServer)
                    {
                        message = String.Format("Job_{0}[{1}] : [{2}]connection has been broken", m_jobId, methodName, time);
                        Trace.WriteLine(message);
                        //this.OnJobEventSend(this, new JobEventArgs(this, message));
                        this.OnFarmConnected(this, new EventArgs());
                    }

                    break;

                default:
                    Trace.WriteLine(String.Format("Job_{0}[{1}] : [{2}]{3}", m_jobId, methodName, time, message.Split('\t')[1]));
                    break;
            }


        }
    }

    public class JobEventArgs : EventArgs
    {
        public object Object { get; }
        public string Message { get; }

        public JobEventArgs(object obj, string message)
        {
            this.Object = obj;
            this.Message = message;
        }
    }

}
