using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using TC4I;

namespace TransactionServer
{
    static class Program
    {
        static void Main()
        {
            Global.Avms = new AVMSCom("127.0.0.1", "admin", "admin");
            Global.Avms.Connect();

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new Service1() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
