using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace TransactionServer.Jobs.SocketServer
{
    class Config : Base.ServiceConfig
    {
        #region Properties

        private string m_Description;
        private string m_Enabled;
        private string m_Assembly;

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

        // Others

        #endregion

        #region Constructor

        public Config()
        {
            NameValueCollection nvc = Base.ServiceTools.GetSection("SocketServer");

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
                    default:
                        break;
                }
            }
        }

        #endregion
    }
}
