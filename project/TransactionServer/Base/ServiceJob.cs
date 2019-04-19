using System;

namespace TransactionServer.Base
{
    /// <summary>
    /// Job Item
    /// </summary>
    public abstract class ServiceJob
    {
        private ServiceConfig m_ConfigObject;
        private DateTime m_NextTime;    // next running time
        protected bool m_IsRunning;

        public ServiceJob m_parentJob = null;
        protected bool m_IsCallbackAdded = false;
        public delegate void JobEventHandler(object sender, JobEventArgs e);
        public event JobEventHandler JobEventSend;

        public bool IsRunning { get { return this.m_IsRunning; } }


        public void OnJobEventSend(object sender, JobEventArgs e)
        {
            if (null != JobEventSend)
            {
                this.JobEventSend(sender, e);
            }
        }

        public void SetParentJob(ServiceJob job)
        {
            m_parentJob = job;
        }


        public ServiceJob()
        {
            this.m_NextTime = DateTime.Now;
            this.m_IsRunning = false;
        }

        public ServiceConfig ConfigObject
        {
            get { return this.m_ConfigObject; }
            set { this.m_ConfigObject = value; }
        }

        public void StartJob()
        {
            if (null == this.ConfigObject)
            {
                return;
            }
            if ((!this.m_IsRunning) && ("true" == this.m_ConfigObject.Enabled.ToLower()))
            {
                if ((null != m_parentJob) && (!m_IsCallbackAdded))
                {
                    this.JobEventSend += new JobEventHandler(m_parentJob.Callback_JobEventSend);
                    m_IsCallbackAdded = true;
                }

                this.Start();
            }
        }

        public void StopJob()
        {
            if ("true" == this.m_ConfigObject.Enabled.ToLower())
            {
                if ((null != m_parentJob) && (m_IsCallbackAdded))
                {
                    this.JobEventSend -= new JobEventHandler(m_parentJob.Callback_JobEventSend);
                    m_IsCallbackAdded = false;
                }
                m_parentJob = null;

                if (this.m_IsRunning)
                {
                    this.Stop();
                }

                this.m_ConfigObject = null;
            }
        }

        public void InitJob()
        {
            this.Init();
        }

        public void CleanJob()
        {
            this.Cleanup();
        }

        #region Interfaces

        protected abstract void Init();
        protected abstract void Cleanup();
        protected abstract void Start();
        protected abstract void Stop();

        protected abstract void Callback_JobEventSend(object sender, JobEventArgs e);

        #endregion
    }
}
