using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Timers;
using System.IO;
using Seer.DeviceModel.Client;

namespace TransactionServer
{
    public class DeviceFilter
    {
        private Dictionary<string, string> m_avms_cameras = null;
        //private Dictionary<string, ACAPCamera> m_acap_cameras = null;
        private List<ACAPCamera> m_acap_cameras = null;
        private List<ACAPCamera> m_acap_avms_cameras = null;
        private Timer m_import_timer = null;
        //private Timer process_timer = null;
        public event EventHandler<EventArgs> ACAPCameraListUpdateEvent;
        public static readonly object utLock = new object();
        private const int IMPORT_INTERVAL = 20 * 1000;
        private const string FILE_FORMAT = ".csv";


        public List<ACAPCamera> GetACAPCameraList()
        {
            return m_acap_avms_cameras;
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
            m_avms_cameras = new Dictionary<string, string>();
            m_acap_cameras = new List<ACAPCamera>();
            m_acap_avms_cameras = new List<ACAPCamera>();

            m_import_timer = new Timer(IMPORT_INTERVAL);
            m_import_timer.Enabled = true;
            m_import_timer.Elapsed += ImportACAPCamera;
        }

        public void UpdateAVMSCameras(Dictionary<uint, CCamera> cameras)
        {
            foreach (CCamera cam in cameras.Values)
            {
                if (!m_avms_cameras.ContainsKey(cam.IPAddress))
                {
                    string ids = cam.CameraId.ToString();
                    m_avms_cameras.Add(cam.IPAddress, ids);
                }
                else
                {
                    string ids = m_avms_cameras[cam.IPAddress];
                    if (string.Empty != ids.Trim())
                    {
                        ids += String.Format(",{0}", cam.CameraId);
                        m_avms_cameras[cam.IPAddress] = ids;
                    }
                    else
                    {
                        ids += cam.CameraId.ToString() ;
                    }
                }
            }

            MakeDevice(m_avms_cameras, m_acap_cameras);
        }

        public void UpdateACAPCameras(ACAPCamera camera, ref bool bChanged)
        {
            var list = m_acap_cameras.Where(cam => cam.ip == camera.ip).ToList();
            switch (list.Count)
            {
                case 0:

                    if ((ACAP_TYPE.ACAP_TYPE_UNKNOWN != camera.type) && (DEVICE_STATE.DEVICE_STATE_UNKNOWN != camera.status))
                    {
                        m_acap_cameras.Add(camera);
                        UpdateFlag(ref bChanged);
                    }
                    break;

                case 1:

                    bool isEqual = list[0].Equals(camera);
                    if (!isEqual)
                    {
                        if ((ACAP_TYPE.ACAP_TYPE_UNKNOWN != camera.type) && (DEVICE_STATE.DEVICE_STATE_UNKNOWN != camera.status))
                        {
                            list.ForEach(cam =>
                            {
                                cam.SetACAPType(camera.type);
                                cam.SetCameraStatus(camera.status);
                            });
                            UpdateFlag(ref bChanged);
                        }
                        //else
                        //{
                        //    m_acap_cameras.RemoveAll(cam => { return cam.ip == camera.ip; });
                        //    UpdateFlag(ref bChanged);
                        //}
                    }
                    break;

                default:

                    m_acap_cameras.RemoveAll(cam => { return cam.ip == camera.ip; });
                    if ((ACAP_TYPE.ACAP_TYPE_UNKNOWN != camera.type) && (DEVICE_STATE.DEVICE_STATE_UNKNOWN != camera.status))
                    {
                        m_acap_cameras.Add(camera);
                        UpdateFlag(ref bChanged);
                    }
                    break;
            }

            MakeDevice(m_avms_cameras, m_acap_cameras);
        }

        private void ImportACAPCamera(Object obj, ElapsedEventArgs args)
        {
            bool isChanged = false;
            Import(System.Windows.Forms.Application.StartupPath.ToString() + @"\ACAPCameras.csv", ref isChanged);
            Trace.WriteLine(String.Format("ACAP Camera List - isChanged = {0}", isChanged));
            Print(m_acap_cameras);
            Trace.WriteLine("ACAP AVMS Camera List : \n");
            Print(m_acap_avms_cameras);
        }

        public static void Print(List<ACAPCamera> list)
        {
            list.ForEach(delegate (ACAPCamera cam)
            {
                Trace.WriteLine(String.Format("cam : ip = {0}, acap_type = {1}, device_state = {2}\n", cam.ip, cam.type, cam.status));
            });
        }

        private void MakeDevice(Dictionary<string, string> listAVMS, List<ACAPCamera> listACAP)
        {
            lock(utLock)
            {
                List<string> avmsIpList = listAVMS.Keys.ToList();
                var list = listACAP.Where(cam => avmsIpList.Contains(cam.ip)).ToList();
                if (0 < list.Count)
                {
                    list.ForEach(cam =>
                    {
                        cam.SetCameraIds(listAVMS[cam.ip]);
                    });
                }
                m_acap_avms_cameras = list;
                this.OnACAPCameraListUpdated(this, new DeviceArgs(this, m_acap_avms_cameras));
            }
        }

        private void Import(string sFilename, ref bool bChanged)
        {
            try
            {
                string log = string.Empty;

                if (!File.Exists(sFilename))
                {
                    log += "File does not exist: " + sFilename;
                    Trace.WriteLine(log);
                    return;
                }

                if (FILE_FORMAT.ToLower() != Path.GetExtension(sFilename).ToLower())
                {
                    log += "Invalid file format : " + sFilename;
                    Trace.WriteLine(log);
                    return;
                }

                Stream stream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(stream);

                string content = sr.ReadToEnd();
                stream.Close();
                if (string.Empty == content)
                {
                    log += "Empty file : " + sFilename;
                    Trace.WriteLine(log);
                    return;
                }

                string[] lines = content.Split('\n');
                foreach (string line in lines)
                {
                    if ((string.Empty == line) || ("\r" == line))
                    {
                        log += String.Format("Invalid line [{0}], skip...\n", line.Trim());
                        continue;
                    }

                    ACAPCamera acap_cam = new ACAPCamera();
                    bool isOK = Process(line, ',', ref acap_cam);
                    if (!isOK)
                    {
                        log += String.Format("Process failed with line [{0}], skip...\n", line.Trim());
                        continue;
                    }
                    else
                    {
                        UpdateACAPCameras(acap_cam, ref bChanged);
                    }
                }
                Trace.WriteLine(log);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("Error importing file \"{0}\" : {1}", sFilename, ex.ToString()));
            }
        }

        private bool Process(string data, char separator, ref ACAPCamera cam)
        {
            string[] info = data.Trim().Split(separator);
            string log = String.Format("Parse data[{0}] into acap camera : ", data.Trim());

            if (3 != info.Length)
            {
                log += "Invalid device structure\n";
                Trace.WriteLine(log);
                return false;
            }

            if ((string.Empty == info[0]) || (string.Empty == info[1]) || (string.Empty == info[2]))
            {
                log += "One empty value at least for device info\n";
                Trace.WriteLine(log);
                return false;
            }

            if (!ValidateIPAddress(info[0]))
            {
                log += String.Format("Invalid ip address data - {0}\n", info[0]);
                Trace.WriteLine(log);
                return false;
            }
            string ip = info[0];
            log += String.Format("ip = {0} with\n", info[0]);
            cam.SetCameraIp(ip);

            int type_id;
            if (!int.TryParse(info[1], out type_id))
            {
                log += String.Format("Invalid acap type data - {0}, acap_type = {1}\n", info[1], cam.type);
            }
            else
            {
                ACAP_TYPE type = (ACAP_TYPE)type_id;
                if (!ValidateEnumType(type))
                {
                    log += String.Format("Invalid acap type data - {0}, acap_type = {1}\n", type, cam.type);
                }
                else
                {
                    log += String.Format("acap_type = {0}\n", type);
                    cam.SetACAPType(type);
                }
            }

            int state_id;
            if (!int.TryParse(info[2], out state_id))
            {
                log += String.Format("Invalid state type data - {0}, device_state = {1}\n", info[2], cam.status);
            }
            else
            {
                DEVICE_STATE state = (DEVICE_STATE)state_id;
                if (!ValidateEnumType(state))
                {
                    log += String.Format("Invalid state type data - {0}, device_state = {1}\n", state, cam.status);
                }
                else
                {
                    log += String.Format("device_state = {0}\n", state);
                    cam.SetCameraStatus(state);
                }
            }

            Trace.WriteLine(log);
            return true;
        }

        private static void UpdateFlag(ref bool flag)
        {
            if (!flag)
            {
                flag = !flag;
            }
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
