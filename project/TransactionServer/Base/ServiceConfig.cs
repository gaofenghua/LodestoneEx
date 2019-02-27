using System;

namespace TransactionServer.Base
{
    /// <summary>
    /// Configuration of Job Item
    /// </summary>
    public abstract class ServiceConfig
    {
        public abstract string Description
        {
            get;
        }

        public abstract string Enabled
        {
            get;
        }

        public abstract string Assembly
        {
            get;
        }

        public abstract string Parent
        {
            get;
        }

        public abstract void Load(string section);
    }
}
