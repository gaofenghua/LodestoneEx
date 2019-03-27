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
        private Hashtable hashJobs;
        private bool m_bTraceLogEnabled = true;
        private bool m_bPrintLogEnabled = false;

        private const string LOG_FILE = "TransactionServer.log";

        public Service1()
        {
            InitializeComponent();

            m_namespaceName = MethodBase.GetCurrentMethod().DeclaringType.Namespace;
            m_className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
            m_bPrintLogEnabled = (ServiceTools.GetAppSetting("print_log_enabled").ToLower() == "true") ? true : false;
        }

        private void PrintLog(string text)
        {
            if (m_bTraceLogEnabled)
            {
                Trace.WriteLine(text);
            }
            if (m_bPrintLogEnabled)
            {
                ServiceTools.WriteLog(System.Windows.Forms.Application.StartupPath.ToString() + @"\" + LOG_FILE, text, true);
            }
        }

        protected override void OnStart(string[] args)
        {
            Thread.Sleep(30 * 1000);
            this.runJobs();
        }

        protected override void OnStop()
        {
            this.stopJobs();
        }

        private void runJobs()
        {
            string prefix = m_className + " - " + MethodBase.GetCurrentMethod().Name;

            try
            {
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
                            string classFullName = string.Empty;
                            if (2 == sectionName.Split('-').Length)
                            {
                                classFullName = assemblyName + ".Jobs." + sectionName.Split('-')[0] + ".Config";
                            }
                            else
                            {
                                classFullName = assemblyName + ".Jobs." + sectionName + ".Config";
                            }

                            ServiceConfig config = Assembly.Load(assemblyName).CreateInstance(classFullName) as ServiceConfig;
                            if (null == config)
                            {
                                PrintLog(String.Format("{0} : fail to load class ({1})", prefix, classFullName));
                                continue;
                            }
                            config.Load(sectionName);
                            ServiceJob job = Assembly.Load(config.Assembly.Split(',')[1]).CreateInstance(config.Assembly.Split(',')[0]) as ServiceJob;
                            job.ConfigObject = config;

                            this.hashJobs.Add(sectionName, job);
                        }
                    }
                }

                if (this.hashJobs.Keys.Count > 0)
                {
                    foreach (ServiceJob job in hashJobs.Values)
                    {
                        ServiceConfig config = job.ConfigObject;
                        string parentName = config.Parent;
                        if (hashJobs.ContainsKey(parentName))
                        {
                            ServiceJob parentJob = (ServiceJob)hashJobs[parentName];
                            job.SetParentJob(parentJob);
                        }

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
                ServiceTools.WindowsServiceStop("TransactionServer");
            }
            RegisterEvents();
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

            try
            {
                job.InitJob();
                job.StartJob();
            }
            catch (Exception e)
            {
                PrintLog(String.Format("Fail to run job[{0}] : {1}", job.ToString(), e.Message));
                ServiceTools.WindowsServiceStop("TransactionServer");
            }
        }
        private void RegisterEvents()
        {
            TransactionServer.Jobs.AVMS.Job Avms = (TransactionServer.Jobs.AVMS.Job)hashJobs["AVMS"];
            TransactionServer.Jobs.Client_Com.Client_Com ClientCom = (TransactionServer.Jobs.Client_Com.Client_Com)hashJobs["Client_Com"];

            if(Avms != null && ClientCom != null)
            {
                if(Avms.m_deviceFilter==null)
                {
                    Thread.Sleep(1000);
                }
                Avms.m_deviceFilter.ACAPCameraListUpdateEvent += ClientCom.OnACAPCameraListUpdate;
            }
        }
    }
}
