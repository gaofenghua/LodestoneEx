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
using TransactionServer.Base;

namespace TransactionServer
{
    public class DeviceFilter
    {
        private Dictionary<string, uint> m_avms_cameras = null;
        private List<ACAPCamera> m_acap_cameras = null;
        private List<ACAPCamera> m_acap_avms_cameras = null;
        private Timer m_import_timer = null;
        public event EventHandler<EventArgs> ACAPCameraListUpdateEvent;
        private string m_workDirectory = string.Empty;
        private string m_acapFile = string.Empty;
        private string m_resultFile = string.Empty;
        private bool m_bTraceLogEnabled = true;
        private bool m_bPrintLogEnabled = false;

        public static readonly object utLock = new object();
        private const int IMPORT_INTERVAL = 30 * 1000;
        private const string FILE_FORMAT = ".csv";
        private const string JOB_LOG_FILE = "TransactionServer.log";


        private void PrintLog(string text)
        {
            if (m_bTraceLogEnabled)
            {
                Trace.WriteLine(text);
            }
            if (m_bPrintLogEnabled)
            {
                ServiceTools.WriteLog(m_workDirectory + @"\" + JOB_LOG_FILE, text, true);
            }
        }

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
            m_bPrintLogEnabled = (ServiceTools.GetAppSetting("print_log_enabled").ToLower() == "true") ? true : false;
            m_avms_cameras = new Dictionary<string, uint>();
            m_acap_cameras = new List<ACAPCamera>();
            m_acap_avms_cameras = new List<ACAPCamera>();
            m_import_timer = new Timer(IMPORT_INTERVAL);
            m_import_timer.Enabled = true;
            m_import_timer.Elapsed += ImportACAPCamera;
            m_workDirectory = System.Windows.Forms.Application.StartupPath.ToString();
            m_acapFile = m_workDirectory + @"\ACAPCameras.csv";
            m_resultFile = m_workDirectory + @"\CameraList.csv";
        }

        public void Create()
        {
            bool isImported = ImportCSV(m_resultFile);
            PrintLog(String.Format("Import CSV : {0}\n{1}", isImported, GetItemsInfo(m_acap_avms_cameras)));
            if (isImported)
            {
                this.OnACAPCameraListUpdated(this, new DeviceArgs(this, m_acap_avms_cameras));
            }
        }

        public void Destroy()
        {
            string info = GetItemsInfo(m_acap_avms_cameras);
            PrintLog(String.Format("Remove DeviceFilter - ExportToCSV : {0}\n{1}", ExportToCSV(m_resultFile, info), info));
            m_bPrintLogEnabled = false;
            m_avms_cameras = null;
            m_acap_cameras = null;
            m_acap_avms_cameras = null;
            m_import_timer.Enabled = false;
            m_import_timer.Elapsed -= ImportACAPCamera;
            m_import_timer = null;
            m_workDirectory = System.Windows.Forms.Application.StartupPath.ToString();
            m_acapFile = string.Empty;
            m_resultFile = string.Empty;
        }

        public void UpdateAVMSCameras(Dictionary<uint, CCamera> cameras)
        {
            string info = string.Empty;
            m_avms_cameras.Clear();
            foreach (CCamera cam in cameras.Values)
            {
                if (!m_avms_cameras.ContainsKey(cam.IPAddress))
                {
                    info += String.Format("cameraIp = {0}, cameraId = {1}\n", cam.IPAddress, cam.CameraId);
                    m_avms_cameras.Add(cam.IPAddress, cam.CameraId);
                }
            }

            PrintLog(String.Format("AVMS Camera List - isChanged = true :\n{0}", info));
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

            PrintLog(String.Format("ACAP Camera List - isChanged = {0} :\n{1}", bChanged, GetItemsInfo(m_acap_cameras)));
            MakeDevice(m_avms_cameras, m_acap_cameras);
        }

        private void ImportACAPCamera(Object obj, ElapsedEventArgs args)
        {
            bool isChanged = false;
            Import(m_acapFile, ref isChanged);
        }

        public string GetItemsInfo(List<ACAPCamera> list)
        {
            string info = string.Empty;
            list.ForEach(delegate (ACAPCamera cam)
            {
                //info += String.Format("cam : ip = {0}, id = {1}, acap_type = {2}, device_state = {3}\n", cam.ip, cam.id, cam.type, cam.status);
                info += String.Format("{0}, {1}, {2}, {3}, {4}\n", cam.ip, cam.type, cam.status, cam.id, cam.name);
            });
            return info;
        }

        private void MakeDevice(Dictionary<string, uint> listAVMS, List<ACAPCamera> listACAP)
        {
            lock(utLock)
            {
                if ((0 == listAVMS.Count) || (0 == listACAP.Count))
                {
                    PrintLog(String.Format("Make device - no action"));
                    return;
                }

                List<string> avmsIpList = listAVMS.Keys.ToList();
                var list = listACAP.Where(cam => avmsIpList.Contains(cam.ip)).ToList();
                if (0 < list.Count)
                {
                    list.ForEach(cam =>
                    {
                        cam.SetCameraId(listAVMS[cam.ip]);
                    });
                }
                m_acap_avms_cameras = list;
                this.OnACAPCameraListUpdated(this, new DeviceArgs(this, m_acap_avms_cameras));
                string info = GetItemsInfo(m_acap_avms_cameras);
                PrintLog(String.Format("Make device - OnACAPCameraListUpdated : true, ExportToCSV : {0}\n{1}", ExportToCSV(m_resultFile, info), info));
            }
        }

        private bool ImportCSV(string sFilename)
        {
            try
            {
                if (!CheckFileCondition(sFilename, FILE_FORMAT))
                {
                    return false;
                }

                Stream stream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(stream);

                string content = sr.ReadToEnd();
                stream.Close();
                if (string.Empty == content)
                {
                    PrintLog("Empty file : " + sFilename);
                    return false;
                }

                List<ACAPCamera> cameraList = new List<ACAPCamera>();
                string[] lines = content.Split('\n');
                foreach (string line in lines)
                {
                    if ((string.Empty == line) || ("\r" == line))
                    {
                        PrintLog(String.Format("Invalid line [{0}], skip...\n", line.Trim()));
                        continue;
                    }

                    ACAPCamera acap_cam = new ACAPCamera();
                    bool isOK = Process(line, ',', ref acap_cam);
                    if (!isOK)
                    {
                        PrintLog(String.Format("Process failed with line [{0}], skip...\n", line.Trim()));
                        continue;
                    }
                    else
                    {
                        var list = cameraList.Where(cam => cam.ip == acap_cam.ip).ToList();
                        if (0 == list.Count)
                        {
                            cameraList.Add(acap_cam);
                        }
                    }
                }
                m_acap_avms_cameras = cameraList;
                return true;
            }
            catch (Exception ex)
            {
                PrintLog(String.Format("Error importing file \"{0}\" : {1}", sFilename, ex.ToString()));
                return false;
            }
        }

        private bool ExportToCSV(string sFilename, string content)
        {
            bool isExported = false;

            StreamWriter sw = new StreamWriter(sFilename, false, Encoding.Unicode);
            try
            {
                sw.WriteLine(content);
                isExported = true;
            }
            catch (Exception ex)
            {
                PrintLog(String.Format("Error exporting file \"{0}\" : {1}", sFilename, ex.ToString()));
                isExported = false;
            }
            sw.Close();

            return isExported;
        }

        private bool CheckFileCondition(string sFilename, string sFileFormat)
        {
            if (!File.Exists(sFilename))
            {
                PrintLog("File does not exist: " + sFilename);
                return false;
            }

            if (sFileFormat.ToLower() != Path.GetExtension(sFilename).ToLower())
            {
                PrintLog("Invalid file format : " + sFilename);
                return false;
            }

            return true;
        }

        private void Import(string sFilename, ref bool bChanged)
        {
            try
            {
                if (!CheckFileCondition(sFilename, FILE_FORMAT))
                {
                    return;
                }

                Stream stream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(stream);

                string content = sr.ReadToEnd();
                stream.Close();
                if (string.Empty == content)
                {
                    PrintLog("Empty file : " + sFilename);
                    return;
                }

                string[] lines = content.Split('\n');
                foreach (string line in lines)
                {
                    if ((string.Empty == line) || ("\r" == line))
                    {
                        PrintLog(String.Format("Invalid line [{0}], skip...\n", line.Trim()));
                        continue;
                    }

                    ACAPCamera acap_cam = new ACAPCamera();
                    bool isOK = Process(line, ',', ref acap_cam);
                    if (!isOK)
                    {
                        PrintLog(String.Format("Process failed with line [{0}], skip...\n", line.Trim()));
                        continue;
                    }
                    else
                    {
                        UpdateACAPCameras(acap_cam, ref bChanged);
                    }
                }
            }
            catch (Exception ex)
            {
                PrintLog(String.Format("Error importing file \"{0}\" : {1}", sFilename, ex.ToString()));
            }
        }

        private bool Process(string data, char separator, ref ACAPCamera cam)
        {
            string[] info = data.Trim().Split(separator);
            string log = String.Format("Parse data[{0}] into acap camera : ", data.Trim());

            if ((3 > info.Length) || (5 < info.Length))
            {
                log += "Invalid device structure\n";
                PrintLog(log);
                return false;
            }

            if ((string.Empty == info[0]) || (string.Empty == info[1]) || (string.Empty == info[2]))
            {
                log += "One empty value at least for device info\n";
                PrintLog(log);
                return false;
            }

            if (!ValidateIPAddress(info[0]))
            {
                log += String.Format("Invalid ip address data - {0}\n", info[0]);
                PrintLog(log);
                return false;
            }
            string ip = info[0];
            log += String.Format("ip = {0} with\n", info[0]);
            cam.SetCameraIp(ip);

            int type_id;
            ACAP_TYPE type;
            if (int.TryParse(info[1], out type_id))
            {
                type = (ACAP_TYPE)type_id;
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
            else if (Enum.TryParse<ACAP_TYPE>(info[1], out type))
            {
                log += String.Format("acap_type = {0}\n", type);
                cam.SetACAPType(type);
            }
            else
            {
                log += String.Format("Invalid acap type data - {0}, acap_type = {1}\n", info[1], cam.type);
            }

            int state_id;
            DEVICE_STATE state;
            if (int.TryParse(info[2], out state_id))
            {
                state = (DEVICE_STATE)state_id;
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
            else if (Enum.TryParse<DEVICE_STATE>(info[2], out state))
            {
                log += String.Format("device_state = {0}\n", state);
                cam.SetCameraStatus(state);
            }
            else
            {
                log += String.Format("Invalid state type data - {0}, device_state = {1}\n", info[2], cam.status);
            }

            if (4 <= info.Length)
            {
                uint device_id;
                if (!uint.TryParse(info[3], out device_id))
                {
                    log += String.Format("Invalid device id data - {0}, device_id = {1}\n", type, cam.id);
                }
                else
                {
                    log += String.Format("device_id = {0}\n", device_id);
                    cam.SetCameraId(device_id);
                }
            }

            if (5 <= info.Length)
            {
                string device_name = info[4];
                log += String.Format("device_name = {0}\n", device_name);
                cam.SetCameraName(device_name);
            }

            PrintLog(log);
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
