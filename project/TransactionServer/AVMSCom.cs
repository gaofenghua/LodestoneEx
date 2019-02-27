using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Data;
using Seer.BaseLibCS;
using Seer.BaseLibCS.Communication;
using Seer.BaseLibCS.SeerWS;
using Seer.SDK;
using Seer.SDK.NotificationMonitors;
using Seer.DeviceModel.Client;
using Seer.Connectivity;
using Seer.Utilities;

namespace TransactionServer
{
    public class AVMSCom
    {
        public string IpAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        private CNetworkAddress ServerAddress
        {
            get { return new CNetworkAddress(IpAddress); }
        }
        private string EncodePassword
        {
            get { return Utils.EncodeString(Password); }
        }

        //
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);    // agent
        public event MessageEventHandler MessageSend;  // event
        //
        private Utils m_utils = new Utils();
        private Thread connectThread = null;
        private ManualResetEvent m_waitForServerInitialized = new ManualResetEvent(false);

        public bool IsConnected
        {
            get { return m_bConnectedToServer; }
        }
        public SdkFarm Farm
        {
            get { return m_farm; }
        }
        public string[] ServerList
        {
            get { return m_servers; }
        }
        public CDeviceManager DeviceManager
        {
            get
            {
                if (null != m_farm)
                {
                    return m_farm.DeviceManager;
                }
                return null;
            }
        }

        private bool m_bConnectedToServer = false;
        private SdkFarm m_farm = null;
        private string[] m_servers = null;
        private List<CCamera> m_listCameras = new List<CCamera>();
        private Dictionary<uint, CCamera> m_mapCameras = new Dictionary<uint, CCamera>();


        public AVMSCom(string strIp, string strUser, string strPassword)
        {
            IpAddress = strIp;
            Username = strUser;
            Password = strPassword;
        }

        #region Connection

        public void Connect()
        {
            connectThread = m_utils.CreateWorkerThread("ConnectToFarm", ConnectToFarm);
        }

        public void Disconnect()
        {
            DestoryFarm();
        }

        private void ConnectToFarm()
        {
            try
            {
                DestoryFarm();

                string sStatus = string.Empty;
                if (string.Empty != (sStatus = LoadFarm()))
                {
                    MessageBox.Show(sStatus);
                    return;
                }
                m_waitForServerInitialized.Set();
                if (!m_waitForServerInitialized.WaitOne(TimeSpan.FromSeconds(60)))
                {
                    MessageBox.Show("Server connection established but server did not initialize within 60 seconds.");
                    DestoryFarm();
                    return;
                }

                m_farm.SetEnabled(true);
                m_bConnectedToServer = true;
                this.OnMessageSend(this, new MessageEventArgs(DateTime.Now.ToString() + "\tConnect"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error connecting to the farm: " + ex.ToString());
                DestoryFarm();
            }
        }


        private void DestoryFarm()
        {
            if (null != m_farm)
            {
                m_farm.SetEnabled(false);
                Thread.Sleep(1000);
                m_farm.Dispose();
                m_farm = null;

                m_bConnectedToServer = false;
                this.OnMessageSend(this, new MessageEventArgs(DateTime.Now.ToString() + "\tDisconnect"));
            }
            m_waitForServerInitialized.Reset();
        }

        private string LoadFarm()
        {
            try
            {
                AttemptConnection();
                m_farm = new SdkFarm(ServerAddress, Username, EncodePassword);
                m_farm.DeviceModelRefreshTrigger = Seer.FarmLib.Client.CFarm.DeviceAutoRefreshTrigger.AnyChange;

                return m_farm.Connect();
            }
            catch (Exception ex)
            {
                return "Failed to connect to farm: " + ex.Message;
            }
        }

        // Verify the credentials and get list of server addresses
        private void AttemptConnection()
        {
            try
            {
                m_servers = null;

                IPEndPoint[] endPoints = null;
                endPoints = Utils.ToEndPoints(IpAddress);
                using (ServerConnectionManager scm = ServerConnectionManager.CreateManager(endPoints, Guid.Empty,
                    new EstablishConnectionOptions(0, TimeSpan.FromSeconds(0))))
                {
                    Seer.BaseLibCS.Proxy.Registration.Registration registrationProxy =
                        scm.GetWebServiceProxy<Seer.BaseLibCS.Proxy.Registration.Registration>();
                    m_servers = registrationProxy.GetAddressesOfServers(Username, EncodePassword);
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new Exception("Not Authorized. Check user name and password. Please make sure the service \"AI Infoservice\" is running on the server and that it is not firewalled. If authenticating against ActiveDirectory you may need to specify <domain>\\<username> (eg microsoft\\bgates).");
            }
            catch (Exception ex)
            {
                string message = string.Empty;
                if (ex.ToString().IndexOf("WebException") >= 0)
                {
                    AILog.Log(LogLevels.LogError, ex.ToString());
                    message = string.Format("{0} [{1}]. {2}",
                        "Server is not online or not reachable",
                        IpAddress,
                        "Please make sure the service \"AI Infoservice\" is running on the server and that it is not firewalled. If authenticating against ActiveDirectory you may need to specify <domain>\\<username> (eg microsoft\\bgates)");
                }
                else
                {
                    AILog.Log(LogLevels.LogError, ex.ToString());

                    message = string.Format("{0} {1}. {2}",
                        "Error: Could not connect to",
                        IpAddress,
                        "Please make sure the service \"AI Infoservice\" is running on the server and that it is not firewalled. If authenticating against ActiveDirectory you may need to specify <domain>\\<username> (eg microsoft\\bgates)");
                }
                throw new Exception(message);
            }
        }

        #endregion


        #region Utility

        public bool MarkAlarm(CCamera cam, uint iAlarmId, bool bAlarmConfirm, string strAlarmComment, ref DataSet result)
        {
            try
            {
                Signals signals = CreateSignals(cam);
                if (null == signals)
                {
                    return false;
                }

                result = signals.MarkAlarm3(Username, Password,  // EncodePassord is also OK
                    iAlarmId,
                    bAlarmConfirm,
                    SqlUtils.SqlEnquote(strAlarmComment),
                    "",
                    true);
                if ((null == result) || (result.Tables.Count <= 0))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fail to mark alarm for server " + cam.Server.Name + ": " + ex.ToString());
                return false;
            }
        }

        public bool ExportSignalsStream(CCamera cam, DateTime dtStart, DateTime dtEnd, ref byte[] result)
        {
            try
            {
                Signals signals = CreateSignals(cam);
                if (null == signals)
                {
                    return false;
                }

                signals.Timeout = 1000 * 60;
                result = signals.GetAlarms(Username, Password,  // EncodePassord is also OK
                    FixDateTimeForWebService(dtStart),
                    FixDateTimeForWebService(dtEnd),
                    string.Empty,
                    1,
                    CommonData.DT_1970);
                if ((null == result) || (result.Length < 1))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fail to retrieve alarm list from server " + cam.Server.Name + ": " + ex.ToString());
                return false;
            }

        }

        public bool ExportImageStream(CCamera cam, DateTime dtJpgTime, bool bViewPrivateVideo, ref string strFilename, ref byte[] result)
        {
            try
            {
                Signals signals = CreateSignals(cam);
                if (null == signals)
                {
                    return false;
                }

                int iStuffedDecoration = 79 + Utils.GetViewPrivateVideoDecoration(bViewPrivateVideo);
                result = signals.GetJPEGImage3(Username, EncodePassword,
                    cam.CameraId,
                    dtJpgTime,
                    0,
                    false,
                    iStuffedDecoration,
                    out strFilename);
                if ((null == result) || (result.Length < 1))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fail to retrieve image stream from server " + cam.Server.Name + ": " + ex.ToString());
                return false;
            }

        }

        public bool AddAlarm(CCamera cam, int alarmTime, int policyID, string alarmText1, string alarmText2)
        {
            try
            {
                Signals ws = CreateSignals(cam);
                if (null == ws)
                {
                    return false;
                }

                return ws.libAddAlarm("", "",
                    (int)cam.CameraId,
                    alarmTime, policyID, alarmText1, alarmText2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        private Signals CreateSignals(CCamera cam)
        {
            if (null != cam)
            {
                Signals signals = cam.Server.Signals;
                return signals;
            }
            else
            {
                MessageBox.Show("Fail to connect camera.");
                return null;
            }
        }

        public DateTime FixDateTimeForWebService(DateTime p_dt)
        {
            try
            {
                return m_farm.FirstServer.ToUtcTime(new DateTime(p_dt.Ticks).ToLocalTime());
            }
            catch
            {
                MessageBox.Show("Failed to convert time to UTC time");
                return p_dt;
            }
        }


        #endregion



        #region Interaction

        public void OnMessageSend(object sender, MessageEventArgs e)
        {
            if (null != MessageSend)
            {
                this.MessageSend(sender, e);
            }
        }

        #endregion
    }


    public class MessageEventArgs : EventArgs
    {
        public string Message;

        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }

}
