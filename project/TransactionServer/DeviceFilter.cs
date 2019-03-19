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
        private Dictionary<uint, CCamera> m_cameraList = null;
        private Timer process_timer = null;

        private const int PROCESS_INTERVAL = 90 * 1000;


        public DeviceFilter()
        {
            m_avms_cameras = new Dictionary<uint, CCamera>();
            m_acap_cameras = new Dictionary<string, ACAPCamera>();
            process_timer = new Timer(CameraFilter, null, PROCESS_INTERVAL, Timeout.Infinite);

            // dummy
            ACAPCamera acap_camera = new ACAPCamera("192.168.77.243", 9);
            acap_camera.type = ACAPCamera.ACAP_TYPE.ACAP_FDFR;
            UpdateACAPCameras(acap_camera);
        }

        public void UpdateAVMSCameras(Dictionary<uint, CCamera> cameras)
        {
            m_avms_cameras = cameras;
        }

        public void UpdateACAPCameras(ACAPCamera cam)
        {
            string cam_ip = cam.ip;
            ACAPCamera.ACAP_TYPE acap_type = cam.type;
            if (m_acap_cameras.ContainsKey(cam_ip))
            {
                if (acap_type != m_acap_cameras[cam_ip].type)
                {
                    m_acap_cameras[cam_ip].type = acap_type;
                }
            }
            else
            {
                m_acap_cameras.Add(cam_ip, cam);
            }
            
        }

        private void CameraFilter(Object obj)
        {
            ProcessCameraList();
        }

        private void ProcessCameraList()
        {

        }

    }

    public class Device
    {
        public string ip { get; protected set; }
        public uint id { get; protected set; }
        public string name { get; set; }
    }

    public class ACAPCamera : Device
    {
        public enum ACAP_TYPE
        {
            ACAP_FDFR = 1,
            ACAP_LPR = 2
        }

        public ACAP_TYPE type { get; set; }

        public ACAPCamera(string ip, uint id)
        {
            this.ip = ip;
            this.id = id;
        }
    }

}
