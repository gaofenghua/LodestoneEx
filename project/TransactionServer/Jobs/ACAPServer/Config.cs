using System;
using System.Configuration;
using System.Collections.Specialized;

using System.Reflection;
using System.Collections;

namespace TransactionServer.Jobs.ACAPServer
{
    public class Config : Base.ServiceConfig
    {
        private string m_Description;
        private string m_Enabled;
        private string m_Assembly;
        private string m_Parent;
        private string m_Ip;
        private string m_Port;
        private string m_AuthInfo;

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

        public string Ip
        {
            get { return this.m_Ip; }
        }
        public string Port
        {
            get { return this.m_Port; }
        }
        public string AuthInfo
        {
            get { return this.m_AuthInfo; }
        }


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
                    case "ip":
                        this.m_Ip = nvc[s].ToString();
                        break;
                    case "port":
                        this.m_Port = nvc[s].ToString();
                        break;
                    case "auth_info":
                        this.m_AuthInfo = nvc[s].ToString();
                        break;
                    default:
                        break;
                }
            }
        }

    }
}
