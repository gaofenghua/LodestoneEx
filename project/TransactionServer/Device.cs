using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionServer
{
    public enum DEVICE_STATE
    {
        DEVICE_OFFLINE = 0,
        DEVICE_ONLINE = 1
    }

    public enum ACAP_TYPE
    {
        ACAP_FDFR = 1,
        ACAP_LPR = 2
    }

    public class Device
    {
        public string ip { get; protected set; }
        public uint id { get; protected set; }
        public string name { get; set; }
        public DEVICE_STATE status { get; set; }
    }

    public class ACAPCamera : Device
    {
        public ACAP_TYPE type { get; protected set; }

        public ACAPCamera(string ip, uint id, ACAP_TYPE type)
        {
            this.ip = ip;
            this.id = id;
        }

        public void SetType(ACAP_TYPE type)
        {
            this.type = type;
        }
    }

    public class DeviceArgs : EventArgs
    {
        public object Object { get; } = null;
        public List<ACAPCamera> Cameras { get; } = null;

        public DeviceArgs(object obj, List<ACAPCamera> cameras)
        {
            this.Object = obj;
            this.Cameras = cameras;
        }
    }
}
