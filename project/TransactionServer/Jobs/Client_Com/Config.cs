using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace TransactionServer.Jobs.Client_Com
{
    class Config : Base.ServiceConfig
    {
        #region Properties

        private string m_Description;
        private string m_Enabled;
        private string m_Assembly;
        private string m_Parent;

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

        #endregion

        #region Method

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

                    default:
                        break;
                }
            }
        }

        #endregion
    }
}
