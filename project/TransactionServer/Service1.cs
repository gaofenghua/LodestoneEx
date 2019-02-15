using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Reflection;
using System.Threading;
// customize
using TransactionServer.Base;


namespace TransactionServer
{
    public partial class Service1 : ServiceBase
    {
        string m_namespaceName = string.Empty;
        string m_className = string.Empty;
        string m_logFile = string.Empty;
        private Hashtable hashJobs;

        // default
        private const string SERVICE_LOG_FILE = "TransactionServer.log";

        public Service1()
        {
            InitializeComponent();

            m_namespaceName = MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            m_className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
            m_logFile = m_namespaceName + ".log";   // SERVICE_LOG_FILE
        }

        protected override void OnStart(string[] args)
        {
            //// debug
            //Thread.Sleep(1000 * 60);
            ////
            this.runJobs();
        }

        protected override void OnStop()
        {
            this.stopJobs();
        }

        private void PrintLog(string text)
        {
            if ("true" != ServiceTools.GetAppSetting("service_log_enabled").ToLower())
            {
                return;
            }
            ServiceTools.WriteLog(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + m_logFile, text, true);
        }

        private void runJobs()
        {
            string prefix = m_className + " - " + MethodBase.GetCurrentMethod().Name;

            try
            {
                // load job
                if (this.hashJobs == null)
                {
                    hashJobs = new Hashtable();

                    XmlNode configSections = ServiceTools.GetConfigSections();
                    foreach (XmlNode section in configSections)
                    {
                        if ("section" == section.Name.ToLower())
                        {
                            string sectionName = section.Attributes["name"].Value.Trim();
                            string sectionType = section.Attributes["type"].Value.Trim();
                            if (string.Empty == sectionType.Split(',')[0])
                            {
                                continue;
                            }
                            string assemblyName = sectionType.Split(',')[1];
                            string classFullName = assemblyName + ".Jobs." + sectionName + ".Config";

                            ServiceConfig config = Assembly.Load(assemblyName).CreateInstance(classFullName) as ServiceConfig;
                            ServiceJob job = Assembly.Load(config.Assembly.Split(',')[1]).CreateInstance(config.Assembly.Split(',')[0]) as ServiceJob;
                            job.ConfigObject = config;

                            this.hashJobs.Add(sectionName, job);
                        }
                    }
                }


                // additional config
                if (hashJobs.ContainsKey("AVMS"))
                {
                    Jobs.AVMS.Job job = (Jobs.AVMS.Job)hashJobs["AVMS"];
                    foreach (string name in hashJobs.Keys)
                    {
                        if (("AVMS" != name) && ("SystemInfo" != name))
                        {
                            job.SetPlugin(name, (ServiceJob)hashJobs[name]);
                        }
                    }
                }


                // run job
                if (this.hashJobs.Keys.Count > 0)
                {
                    foreach (ServiceJob job in hashJobs.Values)
                    {
                        if (System.Threading.ThreadPool.QueueUserWorkItem(threadCallBack, job))
                        {
                            PrintLog(String.Format("{0} : success to run {1}", prefix, job.ToString()));
                        }
                        else
                        {
                            PrintLog(String.Format("{0} : fail to run {1}", prefix, job.ToString()));
                        }
                    }
                }
            }
            catch (Exception error)
            {
                PrintLog(String.Format("{0} : throw exception \"{1}\"", prefix, error.ToString()));
            }
        }

        private void stopJobs()
        {
            if (null != this.hashJobs)
            {
                foreach (ServiceJob job in hashJobs.Values)
                {
                    job.StopJob();
                    job.CleanJob();
                }

                this.hashJobs.Clear();
            }
        }

        private void threadCallBack(Object state)
        {
            ServiceJob job = (ServiceJob)state;
            job.InitJob();
            job.StartJob();
        }
    }
}
