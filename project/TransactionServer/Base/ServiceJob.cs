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
                this.Start();
            }
        }

        public void StopJob()
        {
            this.Stop();
            this.m_IsRunning = false;
            this.m_ConfigObject = null;
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

        #endregion
    }
}
