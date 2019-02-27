using System;
using System.Configuration;
using System.Collections.Specialized;

using System.Reflection;
using System.Collections;

namespace TransactionServer.Jobs.AVMS
{
    public class Config : Base.ServiceConfig
    {
        #region Properties

        private string m_Description;
        private string m_Enabled;
        private string m_Assembly;
        private string m_Parent;
        private string m_Ip;
        private string m_Port;
        private string m_AuthInfo;
        //private string[] m_pluginNames;

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

        //public ArrayList PluginNames
        //{
        //    get
        //    {
        //        ArrayList nameList = new ArrayList();
        //        foreach (string name in this.m_pluginNames)
        //        {
        //            nameList.Add(name);
        //        }
        //        return nameList;
        //    }
        //}


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


        #endregion

        #region Constructor

        public Config()
        {
            //string namespaceName = MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            //string sectionName = namespaceName.Split('.')[namespaceName.Split('.').Length - 1];
            //NameValueCollection nvc = Base.ServiceTools.GetSection(sectionName);

            //foreach (string s in nvc.Keys)
            //{
            //    switch (s.ToLower())
            //    {
            //        case "description":
            //            this.m_Description = nvc[s].ToString();
            //            break;
            //        case "enabled":
            //            this.m_Enabled = nvc[s].ToString();
            //            break;
            //        case "assembly":
            //            this.m_Assembly = nvc[s].ToString();
            //            break;
            //        case "configuration":
            //            this.m_configFile = nvc[s].ToString();
            //            break;
            //        case "log_enabled":
            //            this.m_enabledLog = nvc[s].ToString();
            //            break;
            //        case "system_id":
            //            this.m_systemId = nvc[s].ToString();
            //            break;
            //        case "plugin":
            //            this.m_pluginNames = nvc[s].ToString().Split(',');
            //            break;
            //        default:
            //            break;
            //    }
            //}
        }

        #endregion
    }
}
