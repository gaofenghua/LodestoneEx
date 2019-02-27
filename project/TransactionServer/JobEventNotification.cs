using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionServer
{
    public class JobEventArgs : EventArgs
    {
        public object Object { get; } = null;
        public string Message { get; } = string.Empty;
        public JobEventInfo Info { get; } = null;

        public JobEventArgs(object obj, string message, JobEventInfo info)
        {
            this.Object = obj;
            this.Message = message;
            this.Info = info;
        }
    }

    public class JobEventInfo
    {
        public int event_time { get; } = -1;
        public int camera_id { get; set; } = -1;
        public int policy_id { get; set; } = -1;

        public JobEventInfo(int time)
        {
            this.event_time = time;
        }
    }
}
