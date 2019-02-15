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
        private string m_configFile;
        private string m_enabledLog;
        private string m_systemId;
        private string[] m_pluginNames;

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

        public string Configuration
        {
            get { return this.m_configFile; }
        }
        public string LogEnabled
        {
            get { return this.m_enabledLog; }
        }
        public string SystemId
        {
            get { return this.m_systemId; }
        }

        public ArrayList PluginNames
        {
            get
            {
                ArrayList nameList = new ArrayList();
                foreach (string name in this.m_pluginNames)
                {
                    nameList.Add(name);
                }
                return nameList;
            }
        }

        #endregion

        #region Constructor

        public Config()
        {
            string namespaceName = MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            string sectionName = namespaceName.Split('.')[namespaceName.Split('.').Length - 1];
            NameValueCollection nvc = Base.ServiceTools.GetSection(sectionName);

            foreach (string s in nvc.Keys)
            {
                switch (s.ToLower())
                {
                    case "description":
                        this.m_Description = nvc[s].ToString();
                        break;
                    case "enabled":
                        this.m_Enabled = nvc[s].ToString();
                        break;
                    case "assembly":
                        this.m_Assembly = nvc[s].ToString();
                        break;
                    case "configuration":
                        this.m_configFile = nvc[s].ToString();
                        break;
                    case "log_enabled":
                        this.m_enabledLog = nvc[s].ToString();
                        break;
                    case "system_id":
                        this.m_systemId = nvc[s].ToString();
                        break;
                    case "plugin":
                        this.m_pluginNames = nvc[s].ToString().Split(',');
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion
    }
}
