using System;
using System.Configuration;
using System.Collections.Specialized;

using System.Reflection;

namespace TransactionServer.Jobs.Bosch.IP7400
{
    public class Config : Base.ServiceConfig
    {
        #region Properties

        private string m_Description;
        private string m_Enabled;
        private string m_Assembly;
        private string m_Parent;
        private string m_RcvIp;
        private string m_RcvPort;
        private string m_DevIp;
        private string m_DevPort;

        public override string Description
        {
            get { return this.m_Description; }
        }

        public override string Enabled
        {
            get { return this.m_Enabled; }
        }

        public override string Assembly
        {
            get { return this.m_Assembly; }
        }

        public override string Parent
        {
            get { return this.m_Parent; }
        }

        public string ReceiveIp
        {
            get { return this.m_RcvIp; }
        }
        public string ReceivePort
        {
            get { return this.m_RcvPort; }
        }

        public string DeviceIp
        {
            get { return this.m_DevIp; }
        }
        public string DevicePort
        {
            get { return this.m_DevPort; }
        }


        #endregion


        #region Methods

        public override void Load(string section)
        {
            NameValueCollection nvc = Base.ServiceTools.GetSection(section);
            this.m_Description = section;

            foreach (string s in nvc.Keys)
            {
                switch (s.ToLower())
                {
                    case "enabled":
                        this.m_Enabled = nvc[s].ToString();
                        break;
                    case "assembly":
                        this.m_Assembly = nvc[s].ToString();
                        break;
                    case "parent":
                        this.m_Parent = nvc[s].ToString();
                        break;
                    case "rcv_addr":
                        string[] rcvAddr = nvc[s].ToString().Split(':');
                        if (2 != rcvAddr.Length)
                        {
                            this.m_RcvIp = string.Empty;
                            this.m_RcvPort = string.Empty;
                        }
                        else
                        {
                            this.m_RcvIp = rcvAddr[0];
                            this.m_RcvPort = rcvAddr[1];
                        }
                        break;
                    case "dev_addr":
                        string[] DevAddr = nvc[s].ToString().Split(':');
                        if (2 != DevAddr.Length)
                        {
                            this.m_DevIp = string.Empty;
                            this.m_DevPort = string.Empty;
                        }
                        else
                        {
                            this.m_DevIp = DevAddr[0];
                            this.m_DevPort = DevAddr[1];
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

    }
}