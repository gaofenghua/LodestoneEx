using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Seer.DeviceModel.Client;

namespace TransactionServer
{
    public class DeviceFilter
    {
        private Dictionary<uint, CCamera> m_avms_cameras = null;
        private Dictionary<string, ACAPCamera> m_acap_cameras = null;
        private List<ACAPCamera> m_acap_listCameras = null;
        private Timer process_timer = null;
        public event EventHandler<EventArgs> ACAPCameraListUpdateEvent;

        private const int PROCESS_INTERVAL = 90 * 1000;
        private const string FILE_FORMAT = ".csv";


        public List<ACAPCamera> ACAPCameraList
        {
            get { return m_acap_listCameras; }
        }

        public void OnACAPCameraListUpdated(object sender, DeviceArgs e)
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
            ACAPCamera acap_camera = new ACAPCamera("192.168.77.243", 9, ACAP_TYPE.ACAP_FDFR);
            //acap_camera.type = ACAPCamera.ACAP_TYPE.ACAP_FDFR;
            UpdateACAPCameras(acap_camera);

            m_acap_listCameras = new List<ACAPCamera>();
            
        }

        public void UpdateAVMSCameras(Dictionary<uint, CCamera> cameras)
        {
            m_avms_cameras = cameras;

            ProcessCameraList();
            this.OnACAPCameraListUpdated(this, new DeviceArgs(this, m_acap_listCameras));
        }

        public void UpdateACAPCameras(ACAPCamera cam)
        {
            string cam_ip = cam.ip;
            ACAP_TYPE acap_type = cam.type;
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
            this.OnACAPCameraListUpdated(this, new DeviceArgs(this, m_acap_listCameras));
        }

        private void ACAPCameraFilter(Object obj)
        {
            ProcessCameraList();
            this.OnACAPCameraListUpdated(this, new DeviceArgs(this, m_acap_listCameras));
        }

        private void ProcessCameraList()
        {

        }

        private string Import(string sFilename, ref bool bChanged)
        {
            //bSaveRequired = false;
            try
            {
                if (!File.Exists(sFilename))
                {
                    return "File does not exist: " + sFilename;
                }

                if (FILE_FORMAT.ToLower() != Path.GetExtension(sFilename).ToLower())
                {
                    return "Invalid file format : " + sFilename;
                }

                Stream stream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(stream);

                string content = sr.ReadToEnd();
                stream.Close();
                if (string.Empty == content)
                {
                    return "Empty file : " + sFilename;
                }
                string[] lines = content.Split('\n');
                string log = string.Empty;
                foreach (string line in lines)
                {
                    string[] parts = line.Trim().Split(',');
                    if ((string.Empty == line) || ("\r" == line) || 0 == parts.Length)
                    {
                        log += String.Format("Invalid line [{0}], skip...\n", line);
                        continue;
                    }
                    if (!Process(parts, ref bChanged))
                    {
                        log += String.Format("Process failed with line [{0}], skip...\n", line);
                        continue;
                    }
                }
                return log;
            }
            catch (Exception ex)
            {
                return String.Format("Error importing file \"{0}\" : {1}", sFilename, ex.ToString());
            }
        }

        private bool Process(string[] info, ref bool bChanged)
        {

            return true;
        }

    }
}
