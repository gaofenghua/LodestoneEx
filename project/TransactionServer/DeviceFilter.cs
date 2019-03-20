using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Seer.DeviceModel.Client;

namespace TransactionServer
{
    public class DeviceFilter
    {
        private Dictionary<uint, CCamera> m_avms_cameras = null;
        private Dictionary<string, ACAPCamera> m_acap_cameras = null;
        private Dictionary<uint, ACAPCamera> m_cameraList = null;
        private List<ACAPCamera> m_acap_listCameras = null;
        private Timer process_timer = null;
        public event EventHandler<EventArgs> ACAPCameraListUpdateEvent;

        private const int PROCESS_INTERVAL = 90 * 1000;


        public List<ACAPCamera> ACAPCameraList
        {
            get { return m_acap_listCameras; }
        }

        public void OnACAPCameraListUpdated(object sender, EventArgs e)
        {
            if (null != ACAPCameraListUpdateEvent)
            {
                this.ACAPCameraListUpdateEvent(sender, e);
            }
        }

        public DeviceFilter()
        {
            m_avms_cameras = new Dictionary<uint, CCamera>();
            m_acap_cameras = new Dictionary<string, ACAPCamera>();
            process_timer = new Timer(ACAPCameraFilter, null, PROCESS_INTERVAL, Timeout.Infinite);

            // dummy
            ACAPCamera acap_camera = new ACAPCamera("192.168.77.243", 9, ACAPCamera.ACAP_TYPE.ACAP_FDFR);
            //acap_camera.type = ACAPCamera.ACAP_TYPE.ACAP_FDFR;
            UpdateACAPCameras(acap_camera);

            m_acap_listCameras = new List<ACAPCamera>();
            
        }

        public void UpdateAVMSCameras(Dictionary<uint, CCamera> cameras)
        {
            m_avms_cameras = cameras;

            ProcessCameraList();
            this.OnACAPCameraListUpdated(this, new EventArgs());
        }

        public void UpdateACAPCameras(ACAPCamera cam)
        {
            string cam_ip = cam.ip;
            ACAPCamera.ACAP_TYPE acap_type = cam.type;
            if (m_acap_cameras.ContainsKey(cam_ip))
            {
                if (acap_type != m_acap_cameras[cam_ip].type)
                {
                    m_acap_cameras[cam_ip].SetType(acap_type);
                }
            }
            else
            {
                m_acap_cameras.Add(cam_ip, cam);
            }

            ProcessCameraList();
            this.OnACAPCameraListUpdated(this, new EventArgs());
        }

        private void ACAPCameraFilter(Object obj)
        {
            ProcessCameraList();
            this.OnACAPCameraListUpdated(this, new EventArgs());
        }

        private void ProcessCameraList()
        {

        }

    }

    public class Device
    {
        public enum DEVICE_STATE
        {
            DEVICE_OFFLINE = 0,
            DEVICE_ONLINE = 1
        }

        public string ip { get; protected set; }
        public uint id { get; protected set; }
        public string name { get; set; }
        public DEVICE_STATE status { get; set; }
    }

    public class ACAPCamera : Device
    {
        public enum ACAP_TYPE
        {
            ACAP_FDFR = 1,
            ACAP_LPR = 2
        }

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

}
