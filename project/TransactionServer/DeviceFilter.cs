using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using Seer.DeviceModel.Client;

namespace TransactionServer
{
    public class DeviceFilter
    {
        private Dictionary<string, List<uint>> m_avms_cameras = null;
        private Dictionary<string, ACAPCamera> m_acap_cameras = null;
        private Timer process_timer = null;
        public event EventHandler<EventArgs> ACAPCameraListUpdateEvent;

        private const int PROCESS_INTERVAL = 90 * 1000;
        private const string FILE_FORMAT = ".csv";


        public void OnACAPCameraListUpdated(object sender, DeviceArgs e)
        {
            if (null != ACAPCameraListUpdateEvent)
            {
                this.ACAPCameraListUpdateEvent(sender, e);
            }
        }

        public DeviceFilter()
        {
            m_avms_cameras = new Dictionary<string, List<uint>>();
            m_acap_cameras = new Dictionary<string, ACAPCamera>();
            process_timer = new Timer(ImportACAPCamera, null, PROCESS_INTERVAL, Timeout.Infinite);

            // dummy
            ACAPCamera acap_camera = new ACAPCamera("192.168.77.243", 9, ACAP_TYPE.ACAP_FDFR);
            //acap_camera.type = ACAPCamera.ACAP_TYPE.ACAP_FDFR;
            UpdateACAPCameras(acap_camera);
        }

        public void UpdateAVMSCameras(Dictionary<uint, CCamera> cameras)
        {
            foreach (CCamera cam in cameras.Values)
            {
                if (!m_avms_cameras.ContainsKey(cam.IPAddress))
                {
                    List<uint> ids = new List<uint>();
                    ids.Add(cam.CameraId);
                    m_avms_cameras.Add(cam.IPAddress, ids);
                }
                else
                {
                    List<uint> ids = m_avms_cameras[cam.IPAddress];
                    if (null != ids)
                    {
                        ids.Add(cam.CameraId);
                        m_avms_cameras[cam.IPAddress] = ids;
                    }
                }
            }

            MakeDevice(m_avms_cameras, m_acap_cameras);
        }

        public void UpdateACAPCameras(ACAPCamera camera)
        {
            string cam_ip = camera.ip;
            ACAP_TYPE acap_type = camera.type;
            if (m_acap_cameras.ContainsKey(cam_ip))
            {
                if (acap_type != m_acap_cameras[cam_ip].type)
                {
                    m_acap_cameras[cam_ip].SetType(acap_type);
                }
            }
            else
            {
                m_acap_cameras.Add(cam_ip, camera);
            }

            //MakeDevice();
        }

        private void ImportACAPCamera(Object obj)
        {
            bool isChanged = false;
            Import(@"E:\BAZZI\GIT\GitHub\Indy\LodestoneEx\output\bin\Debug\ACAPCameras.csv", ref isChanged);
        }

        private void MakeDevice(Dictionary<string, List<uint>> listAll, Dictionary<string, ACAPCamera> listACAP)
        {
            List<ACAPCamera> cameras = new List<ACAPCamera>();
            foreach (KeyValuePair<string, ACAPCamera> pair in listACAP)
            {
                if (listAll.ContainsKey(pair.Key))
                {
                    ACAPCamera cam = pair.Value;
                    cameras.Add(cam);
                }
            }
            this.OnACAPCameraListUpdated(this, new DeviceArgs(this, cameras));
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
            string log = string.Empty;

            if (3 != info.Length)
            {
                log = "Invalid device structure";
                bChanged = false;
                return false;
            }

            if ((string.Empty == info[0]) || (string.Empty == info[1]) || (string.Empty == info[2]))
            {
                log = "One empty value at least for device info";
                bChanged = false;
                return false;
            }

            if (!ValidateIPAddress(info[0]))
            {
                log = String.Format("Invalid ip address data - {0}", info[0]);
                bChanged = false;
                return false;
            }
            string ip = info[0];

            int type_id;
            if (!int.TryParse(info[1], out type_id))
            {
                log = String.Format("Invalid acap type data - {0}", info[1]);
                bChanged = false;
                return false;
            }
            ACAP_TYPE type = (ACAP_TYPE)type_id;
            if (!ValidateEnumType(type))
            {
                log = String.Format("Invalid acap type data - {0}", type);
                bChanged = false;
                return false;
            }

            int state_id;
            if (!int.TryParse(info[2], out state_id))
            {
                log = String.Format("Invalid state type data - {0}", info[2]);
                bChanged = false;
                return false;
            }
            DEVICE_STATE state = (DEVICE_STATE)state_id;
            if (!ValidateEnumType(state))
            {
                log = String.Format("Invalid state type data - {0}", state);
                bChanged = false;
                return false;
            }


            return true;
        }

        public static bool ValidateIPAddress(string data)
        {
            Regex regex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            return (data != "" && regex.IsMatch(data.Trim())) ? true : false;
        }

        public static bool ValidateEnumType<T>(T type)
        {
            Type t = typeof(T);
            Array vals = Enum.GetValues(t);
            int pos = Array.IndexOf(vals, type);
            if (-1 >= pos)
            {
                return false;
            }
            return true;
        }

    }
}
