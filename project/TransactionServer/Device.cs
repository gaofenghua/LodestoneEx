using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionServer
{
    public enum DEVICE_STATE
    {
        DEVICE_STATE_UNKNOWN = 0,
        DEVICE_OFFLINE,
        DEVICE_ONLINE,
    }

    public enum ACAP_TYPE
    {
        ACAP_TYPE_UNKNOWN = 0,
        ACAP_FDFR,
        ACAP_LPR,
    }

    public class Device
    {
        public string ip { get; protected set; } = string.Empty;
        public uint id { get; protected set; } = 0;
        public string name { get; protected set; } = string.Empty;
        public DEVICE_STATE status { get; protected set; } = DEVICE_STATE.DEVICE_STATE_UNKNOWN;
    }

    public class ACAPCamera : Device
    {
        public ACAP_TYPE type { get; protected set; }

        public void SetCameraIp(string ip)
        {
            this.ip = ip;
        }

        public void SetCameraId(uint id)
        {
            this.id = id;
        }

        public void SetCameraName(string name)
        {
            this.name = name;
        }

        public void SetCameraStatus(DEVICE_STATE status)
        {
            this.status = status;
        }

        public void SetACAPType(ACAP_TYPE type)
        {
            this.type = type;
        }


        public override bool Equals(object obj)
        {
            return this.Equals(obj as ACAPCamera);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public bool Equals(ACAPCamera cam)
        {
            return (this.ip == cam.ip) 
                && (this.type == cam.type) 
                && (this.status == cam.status);
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
