using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionServer
{
    public class JobEventArgs : EventArgs
    {
        public object Object { get; }
        public string Message { get; }

        public JobEventArgs(object obj, string message)
        {
            this.Object = obj;
            this.Message = message;
        }
    }
}
