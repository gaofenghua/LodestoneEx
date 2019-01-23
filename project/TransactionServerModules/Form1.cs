using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

using BaseIDL;
using Seer.BaseLibCS;
using Seer.BaseLibCS.Communication;
using Seer.BaseLibCS.SeerWS;
using DeviceModel;
using DeviceModel.Client;
using Seer.FarmLib.Client;
using Seer.FarmLib;
using ICSharpCode.SharpZipLib.GZip;
using Seer;
using Seer.Connectivity;
using Seer.SDK;
using Seer.SDK.NotificationMonitors;
using Seer.Configuration;
using Seer.Database;
using Seer.Database.BaseLibCS;
using Seer.DeviceModel.Client;
using Seer.DeviceModel;
using Seer.Utilities;
using VideoWallLib.Client;
using SeerInterfaces;
using UserModel;
using VideoWallLib;
using SecurityLib;
using SecurityLib.Client;
using BaseLibCS;
using Seer.VideoExport;
using Seer.VideoExport.UI;
using Seer.VideoExport.Exporters;
using Seer.VideoExport.Exporters.UI;
using Seer.Internationalization;
//
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
// libcurl
using SeasideResearch.LibCurlNet;
// json
using Newtonsoft.Json;

using TransactionServerModules.Configuration;


namespace TransactionServerModules
{
    public class Form1 : System.Windows.Forms.Form
    {
        #region Member Variables

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.TextBox tbUser;
        private System.Windows.Forms.TextBox tbPass;
        private System.Windows.Forms.Label lblPass;
        private System.Windows.Forms.TextBox tbIp;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnMarkAlarm;
        private System.Windows.Forms.ListBox listAlarms;
        private System.Windows.Forms.CheckBox cbAlarmFalsePositive;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbAlarmComment;
        private System.Windows.Forms.Button btnDisconnect;
        private IContainer components = null;

        private Utils m_utils = new Utils();
        private AVMSCom m_avms = null;
        private CCamera m_camera = null;
        private AutoResetEvent m_waitForServerConnection = new AutoResetEvent(false);
        private bool m_bConnectedToAVMSServer = false;
        private int m_iCameraId = -1;
        private int m_iServerId = -1;
        private bool m_bListened = false;
        private bool m_bViewPrivateVideo;
        private Button btnRefreshAlarms;
        private bool m_bDeviceModelEventHandlerAdded = false;
        private ManualResetEvent m_waitForServerInitialized = new ManualResetEvent(false);
        private System.Windows.Forms.Timer _runTimeTimer = new System.Windows.Forms.Timer();
        private AlarmMonitor m_alarmMonitor;
        // Access event could not be supported in AlarmMonitor
        private EventMonitor m_eventMonitor;
        //
        private DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private TextBox tbAlarmMessage;
        private ComboBox cbServers;
        private Panel panel3;
        private Button bnResumeRecording;
        private Button btnStopRecording;
        private Button btnStartRecording;
        private GroupBox gbInsertAlarm;
        private Label lblAlarmTime;
        private CheckBox cbCurrentTime;
        private TextBox tbUTCTime;
        private Label lblPolicyID;
        private Label lblAlarmText2;
        private Label lblAlarmText1;
        private TextBox tbAlarmText2;
        private TextBox tbAlarmText1;
        private Button btnInsertAlarm;
        private Label label17;
        private NumericUpDown nudPolicyID;
        private CheckBox checkRecording;
        private GroupBox gbLogin;
        private GroupBox gbDevice;
        private Button btnGetServers;
        private Button btnGetDevices;
        private ListView lvDevices;
        private ColumnHeader colId;
        private ColumnHeader colName;
        private GroupBox gbReceiveAlarm;
        private Button btnListen;
        private TextBox tbAlarmType;
        private Label lblAlarmType;
        private ListBox listRuleAction;
        private TextBox tbRuleAction;
        private Label lblRuleAction;
        private GroupBox gbHTTPRequest;
        private GroupBox gbAVMSFuntion;
        private CheckBox checkPopup;
        private Button btnSnapshot;
        private Button btnClearAlarms;
        private Button btnTest;
        private TextBox tbURL;
        private Label lblURL;
        private TextBox tbBody;
        private Label lblBody;
        private Button btnSend;
        //private bool m_bCameraListPopulating = false;
        private bool m_bServerListPopulating = false;
        private TextBox tbCameraID;
        private Label lblCameraID;
        private DateTimePicker dtStartTime;
        private DateTimePicker dtStopTime;
        private Button btnParseAlarm;
        private TextBox tbPolicyType;
        private Label lblPolicyType;
        private Button btnProcessAlarm;
        private Button btnFilter;

        private bool m_bAVMSListenerEventHandlerAdded = false;
        private AlarmType m_enumAlarmType = AlarmType.UNKNOWN;
        private string m_strPolicyDesc = string.Empty;
        private string m_strRuleAction= string.Empty;
        private List<string> m_listEventIds = new List<string>();
        private TextBox tbResult;
        private Label lblResult;
        private ComboBox cbExecuteAction;
        private Dictionary<int, ArrayList> m_mapActionEvents = new Dictionary<int, ArrayList>();
        private uint m_iExecuteActionValue = 0;

        // focus flag
        private bool m_bNeedFocus = false;
        // test flag
        private bool m_bAutoTest = false;
        // database flag
        private bool m_bDatabaseAccessAllowed = false;


        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private System.Windows.Forms.Timer timer1;
        private Panel panel4;
        private GroupBox gbJobs;
        private ListView lvJobs;
        private ColumnHeader colJobID;
        private ColumnHeader colJobName;
        private ColumnHeader colAVMS;
        private GroupBox gbConfiguration;
        private Button btnImportJob;

        // default value
        private const string RULE_EVENT_CONFIG = "transaction.conf";
        private Button btnClearJob;

        private delegate void DelegateVoid();
        private delegate void DelegateBool(bool bEnable);
        private delegate void DelegateVoid2(Control obj, bool bEnable);
        private delegate void DelegateBool2(bool bEnable1, bool bEnable2);

        public delegate void MessageHandler(MessageEventArgs e);
        public delegate void JobEventHandler(object sender, JobEventArgs e);

        private Hashtable hashJobs;
        //private int m_iJobId = -1;
        private System.Windows.Forms.Timer timer2;
        private RadioButton rbOverall;
        private RadioButton rbMultiJob;
        private ServiceJob currentJob = null;


        static ushort[] CRC16Table =
        {
            0x0, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7,
           0x8108, 0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF,
           0x1231, 0x210, 0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6,
           0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE,
           0x2462, 0x3443, 0x420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485,
           0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D,
           0x3653, 0x2672, 0x1611, 0x630, 0x76D7, 0x66F6, 0x5695, 0x46B4,
           0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D, 0xC7BC,
           0x48C4, 0x58E5, 0x6886, 0x78A7, 0x840, 0x1861, 0x2802, 0x3823,
           0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
           0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0xA50, 0x3A33, 0x2A12,
           0xDBFD, 0xCBDC, 0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A,
           0x6CA6, 0x7C87, 0x4CE4, 0x5CC5, 0x2C22, 0x3C03, 0xC60, 0x1C41,
           0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B, 0x8D68, 0x9D49,
           0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0xE70,
           0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78,
           0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F,
           0x1080, 0xA1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
           0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E,
           0x2B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
           0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D,
           0x34E2, 0x24C3, 0x14A0, 0x481, 0x7466, 0x6447, 0x5424, 0x4405,
           0xA7DB, 0xB7FA, 0x8799, 0x97B8, 0xE75F, 0xF77E, 0xC71D, 0xD73C,
           0x26D3, 0x36F2, 0x691, 0x16B0, 0x6657, 0x7676, 0x4615, 0x5634,
           0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB,
           0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x8E1, 0x3882, 0x28A3,
           0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A,
           0x4A75, 0x5A54, 0x6A37, 0x7A16, 0xAF1, 0x1AD0, 0x2AB3, 0x3A92,
           0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9,
           0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0xCC1,
           0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8,
           0x6E17, 0x7E36, 0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0xED1, 0x1EF0
        };

        #endregion

        #region Constructors

        public Form1(string[] args)
        {
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            ////
            //SetConnectButton(true, m_bConnectedToAVMSServer);
            //UpdateControls(m_bConnectedToAVMSServer);

            // show alarm time
            GetUTCTime();
            tbUTCTime.Enabled = !cbCurrentTime.Checked;

            // search duration for alarms
            dtStartTime.Text = DateTime.Now.ToString();
            dtStopTime.Text = dtStartTime.Text;

            // Read user settings.
            if (args.Length >= 1) tbUser.Text = args[0];
            if (args.Length >= 2) tbPass.Text = args[1];
            if (args.Length >= 3) tbIp.Text = args[2];
            if (args.Length >= 4)
            {
                if (!int.TryParse(args[3], out m_iServerId))
                    m_iServerId = -1;
            }
            if (args.Length >= 5)
            {
                if (!int.TryParse(args[4], out m_iCameraId))
                    m_iCameraId = -1;
            }

            //_runTimeTimer.Interval = 1000;
            //_runTimeTimer.Tick += new EventHandler(_runTimeTimer_Tick);

            // initialize combox item
            InitExecuteAction();

            rbOverall.Checked = true;

            this.toolStripStatusLabel3.Alignment = ToolStripItemAlignment.Right;
            this.timer1.Interval = 1000;
            this.timer1.Start();

            this.timer2.Interval = 1300;
            this.timer2.Start();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled Exception: " + e.ExceptionObject.ToString());
        }

        #endregion

        #region Properties

        private string Ip
        {
            get
            {
                return tbIp.Text;
            }
        }

        private string User
        {
            get
            {
                return tbUser.Text;
            }
        }

        private string Password
        {
            get
            {
                //return Utils.EncodeString(tbPass.Text);
                return tbPass.Text;
            }
        }
        private string EncodePassword
        {
            get
            {
                return Utils.EncodeString(tbPass.Text);
            }
        }

        private SdkFarm m_farm
        {
            get
            {
                if (null != currentJob)
                {
                    if (null != currentJob.m_avms)
                    {
                        return currentJob.m_avms.Farm;
                    }
                    return null;
                }

                if (null != m_avms)
                {
                    return m_avms.Farm;
                }
                return null;
            }
        }

        private CDeviceManager m_deviceManager
        {
            get
            {
                if (null != currentJob)
                {
                    if (null != m_farm)
                    {
                        return m_farm.DeviceManager;
                    }
                    return null;
                }

                if (null != m_farm)
                {
                    return m_farm.DeviceManager;
                }
                return null;
            }
        }

        public struct CameraLogStruct
        {
            public uint m_iAlarmDbId;
            public uint m_iCameraId;
            public uint m_iEvent;
            public uint m_iFarmId;
            public uint m_iFGCount;
            public uint m_iNotUsed2;
            public int m_iPolicyId;
            public uint m_iState;
            public uint m_iVersion;
            public uint m_milliSinceChangeBegan;
            public ushort m_milliTime;
            public short m_timezoneTime;
            public uint m_utcTime;
            //
            public string m_strConfirm;
            public string m_strComment;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.gbConfiguration = new System.Windows.Forms.GroupBox();
            this.rbOverall = new System.Windows.Forms.RadioButton();
            this.rbMultiJob = new System.Windows.Forms.RadioButton();
            this.btnClearJob = new System.Windows.Forms.Button();
            this.btnImportJob = new System.Windows.Forms.Button();
            this.gbJobs = new System.Windows.Forms.GroupBox();
            this.lvJobs = new System.Windows.Forms.ListView();
            this.colJobID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colJobName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colAVMS = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnTest = new System.Windows.Forms.Button();
            this.gbReceiveAlarm = new System.Windows.Forms.GroupBox();
            this.dtStopTime = new System.Windows.Forms.DateTimePicker();
            this.dtStartTime = new System.Windows.Forms.DateTimePicker();
            this.btnClearAlarms = new System.Windows.Forms.Button();
            this.btnListen = new System.Windows.Forms.Button();
            this.listAlarms = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnRefreshAlarms = new System.Windows.Forms.Button();
            this.tbAlarmComment = new System.Windows.Forms.TextBox();
            this.cbAlarmFalsePositive = new System.Windows.Forms.CheckBox();
            this.btnMarkAlarm = new System.Windows.Forms.Button();
            this.gbDevice = new System.Windows.Forms.GroupBox();
            this.lvDevices = new System.Windows.Forms.ListView();
            this.colId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnGetDevices = new System.Windows.Forms.Button();
            this.btnGetServers = new System.Windows.Forms.Button();
            this.cbServers = new System.Windows.Forms.ComboBox();
            this.gbLogin = new System.Windows.Forms.GroupBox();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblUser = new System.Windows.Forms.Label();
            this.tbUser = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblPass = new System.Windows.Forms.Label();
            this.tbIp = new System.Windows.Forms.TextBox();
            this.tbPass = new System.Windows.Forms.TextBox();
            this.lblIp = new System.Windows.Forms.Label();
            this.gbInsertAlarm = new System.Windows.Forms.GroupBox();
            this.tbCameraID = new System.Windows.Forms.TextBox();
            this.lblCameraID = new System.Windows.Forms.Label();
            this.btnInsertAlarm = new System.Windows.Forms.Button();
            this.tbAlarmText2 = new System.Windows.Forms.TextBox();
            this.nudPolicyID = new System.Windows.Forms.NumericUpDown();
            this.tbAlarmText1 = new System.Windows.Forms.TextBox();
            this.lblAlarmText2 = new System.Windows.Forms.Label();
            this.lblAlarmText1 = new System.Windows.Forms.Label();
            this.lblPolicyID = new System.Windows.Forms.Label();
            this.cbCurrentTime = new System.Windows.Forms.CheckBox();
            this.tbUTCTime = new System.Windows.Forms.TextBox();
            this.lblAlarmTime = new System.Windows.Forms.Label();
            this.tbAlarmMessage = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnFilter = new System.Windows.Forms.Button();
            this.btnProcessAlarm = new System.Windows.Forms.Button();
            this.tbPolicyType = new System.Windows.Forms.TextBox();
            this.lblPolicyType = new System.Windows.Forms.Label();
            this.btnParseAlarm = new System.Windows.Forms.Button();
            this.lblRuleAction = new System.Windows.Forms.Label();
            this.listRuleAction = new System.Windows.Forms.ListBox();
            this.tbRuleAction = new System.Windows.Forms.TextBox();
            this.tbAlarmType = new System.Windows.Forms.TextBox();
            this.lblAlarmType = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.gbAVMSFuntion = new System.Windows.Forms.GroupBox();
            this.checkRecording = new System.Windows.Forms.CheckBox();
            this.btnStartRecording = new System.Windows.Forms.Button();
            this.btnStopRecording = new System.Windows.Forms.Button();
            this.checkPopup = new System.Windows.Forms.CheckBox();
            this.bnResumeRecording = new System.Windows.Forms.Button();
            this.btnSnapshot = new System.Windows.Forms.Button();
            this.gbHTTPRequest = new System.Windows.Forms.GroupBox();
            this.cbExecuteAction = new System.Windows.Forms.ComboBox();
            this.lblResult = new System.Windows.Forms.Label();
            this.tbResult = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.tbBody = new System.Windows.Forms.TextBox();
            this.lblBody = new System.Windows.Forms.Label();
            this.tbURL = new System.Windows.Forms.TextBox();
            this.lblURL = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.panel4 = new System.Windows.Forms.Panel();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.panel1.SuspendLayout();
            this.gbConfiguration.SuspendLayout();
            this.gbJobs.SuspendLayout();
            this.gbReceiveAlarm.SuspendLayout();
            this.gbDevice.SuspendLayout();
            this.gbLogin.SuspendLayout();
            this.gbInsertAlarm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPolicyID)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.gbAVMSFuntion.SuspendLayout();
            this.gbHTTPRequest.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.gbConfiguration);
            this.panel1.Controls.Add(this.gbJobs);
            this.panel1.Location = new System.Drawing.Point(4, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(594, 220);
            this.panel1.TabIndex = 0;
            // 
            // gbConfiguration
            // 
            this.gbConfiguration.Controls.Add(this.rbOverall);
            this.gbConfiguration.Controls.Add(this.rbMultiJob);
            this.gbConfiguration.Controls.Add(this.btnClearJob);
            this.gbConfiguration.Controls.Add(this.btnImportJob);
            this.gbConfiguration.Location = new System.Drawing.Point(4, 3);
            this.gbConfiguration.Name = "gbConfiguration";
            this.gbConfiguration.Size = new System.Drawing.Size(583, 54);
            this.gbConfiguration.TabIndex = 9;
            this.gbConfiguration.TabStop = false;
            this.gbConfiguration.Text = "&Config Option";
            // 
            // rbOverall
            // 
            this.rbOverall.AutoSize = true;
            this.rbOverall.Location = new System.Drawing.Point(394, 25);
            this.rbOverall.Name = "rbOverall";
            this.rbOverall.Size = new System.Drawing.Size(65, 16);
            this.rbOverall.TabIndex = 3;
            this.rbOverall.TabStop = true;
            this.rbOverall.Text = "Overall";
            this.rbOverall.UseVisualStyleBackColor = true;
            this.rbOverall.CheckedChanged += new System.EventHandler(this.rbOverall_CheckedChanged);
            // 
            // rbMultiJob
            // 
            this.rbMultiJob.AutoSize = true;
            this.rbMultiJob.Location = new System.Drawing.Point(306, 25);
            this.rbMultiJob.Name = "rbMultiJob";
            this.rbMultiJob.Size = new System.Drawing.Size(77, 16);
            this.rbMultiJob.TabIndex = 2;
            this.rbMultiJob.TabStop = true;
            this.rbMultiJob.Text = "Multi-Job";
            this.rbMultiJob.UseVisualStyleBackColor = true;
            this.rbMultiJob.CheckedChanged += new System.EventHandler(this.rbMultiJob_CheckedChanged);
            // 
            // btnClearJob
            // 
            this.btnClearJob.Location = new System.Drawing.Point(118, 20);
            this.btnClearJob.Name = "btnClearJob";
            this.btnClearJob.Size = new System.Drawing.Size(90, 25);
            this.btnClearJob.TabIndex = 1;
            this.btnClearJob.Text = "Clear Job";
            this.btnClearJob.UseVisualStyleBackColor = true;
            this.btnClearJob.Click += new System.EventHandler(this.btnClearJob_Click);
            // 
            // btnImportJob
            // 
            this.btnImportJob.Location = new System.Drawing.Point(17, 20);
            this.btnImportJob.Name = "btnImportJob";
            this.btnImportJob.Size = new System.Drawing.Size(90, 25);
            this.btnImportJob.TabIndex = 0;
            this.btnImportJob.Text = "Import Job";
            this.btnImportJob.UseVisualStyleBackColor = true;
            this.btnImportJob.Click += new System.EventHandler(this.btnImportJob_Click);
            // 
            // gbJobs
            // 
            this.gbJobs.Controls.Add(this.lvJobs);
            this.gbJobs.Location = new System.Drawing.Point(4, 60);
            this.gbJobs.Name = "gbJobs";
            this.gbJobs.Size = new System.Drawing.Size(583, 152);
            this.gbJobs.TabIndex = 8;
            this.gbJobs.TabStop = false;
            this.gbJobs.Text = "&Job Pool";
            // 
            // lvJobs
            // 
            this.lvJobs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colJobID,
            this.colJobName,
            this.colAVMS});
            this.lvJobs.FullRowSelect = true;
            this.lvJobs.Location = new System.Drawing.Point(17, 20);
            this.lvJobs.MultiSelect = false;
            this.lvJobs.Name = "lvJobs";
            this.lvJobs.Size = new System.Drawing.Size(548, 122);
            this.lvJobs.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvJobs.TabIndex = 0;
            this.lvJobs.UseCompatibleStateImageBehavior = false;
            this.lvJobs.View = System.Windows.Forms.View.Details;
            this.lvJobs.SelectedIndexChanged += new System.EventHandler(this.lvJobs_SelectedIndexChanged);
            // 
            // colJobID
            // 
            this.colJobID.Text = "ID";
            this.colJobID.Width = 30;
            // 
            // colJobName
            // 
            this.colJobName.Text = "Name";
            this.colJobName.Width = 80;
            // 
            // colAVMS
            // 
            this.colAVMS.Text = "AVMS Connection";
            this.colAVMS.Width = 200;
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(304, 348);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(90, 25);
            this.btnTest.TabIndex = 43;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // gbReceiveAlarm
            // 
            this.gbReceiveAlarm.Controls.Add(this.dtStopTime);
            this.gbReceiveAlarm.Controls.Add(this.dtStartTime);
            this.gbReceiveAlarm.Controls.Add(this.btnClearAlarms);
            this.gbReceiveAlarm.Controls.Add(this.btnListen);
            this.gbReceiveAlarm.Controls.Add(this.listAlarms);
            this.gbReceiveAlarm.Controls.Add(this.label5);
            this.gbReceiveAlarm.Controls.Add(this.btnRefreshAlarms);
            this.gbReceiveAlarm.Controls.Add(this.tbAlarmComment);
            this.gbReceiveAlarm.Controls.Add(this.cbAlarmFalsePositive);
            this.gbReceiveAlarm.Controls.Add(this.btnMarkAlarm);
            this.gbReceiveAlarm.Location = new System.Drawing.Point(251, 184);
            this.gbReceiveAlarm.Name = "gbReceiveAlarm";
            this.gbReceiveAlarm.Size = new System.Drawing.Size(581, 238);
            this.gbReceiveAlarm.TabIndex = 42;
            this.gbReceiveAlarm.TabStop = false;
            this.gbReceiveAlarm.Text = "&Receive Alarm";
            // 
            // dtStopTime
            // 
            this.dtStopTime.Location = new System.Drawing.Point(289, 23);
            this.dtStopTime.Name = "dtStopTime";
            this.dtStopTime.Size = new System.Drawing.Size(103, 21);
            this.dtStopTime.TabIndex = 37;
            // 
            // dtStartTime
            // 
            this.dtStartTime.Location = new System.Drawing.Point(184, 23);
            this.dtStartTime.Name = "dtStartTime";
            this.dtStartTime.Size = new System.Drawing.Size(103, 21);
            this.dtStartTime.TabIndex = 36;
            this.dtStartTime.Value = new System.DateTime(2018, 8, 1, 11, 24, 50, 0);
            // 
            // btnClearAlarms
            // 
            this.btnClearAlarms.Location = new System.Drawing.Point(491, 23);
            this.btnClearAlarms.Name = "btnClearAlarms";
            this.btnClearAlarms.Size = new System.Drawing.Size(83, 21);
            this.btnClearAlarms.TabIndex = 35;
            this.btnClearAlarms.Text = "Clear";
            this.btnClearAlarms.UseVisualStyleBackColor = true;
            this.btnClearAlarms.Click += new System.EventHandler(this.btnClearAlarms_Click);
            // 
            // btnListen
            // 
            this.btnListen.Enabled = false;
            this.btnListen.Location = new System.Drawing.Point(14, 19);
            this.btnListen.Name = "btnListen";
            this.btnListen.Size = new System.Drawing.Size(129, 25);
            this.btnListen.TabIndex = 0;
            this.btnListen.Text = "Start Listener";
            this.btnListen.UseVisualStyleBackColor = true;
            this.btnListen.Click += new System.EventHandler(this.btnListen_Click);
            // 
            // listAlarms
            // 
            this.listAlarms.FormattingEnabled = true;
            this.listAlarms.ItemHeight = 12;
            this.listAlarms.Location = new System.Drawing.Point(7, 47);
            this.listAlarms.Name = "listAlarms";
            this.listAlarms.Size = new System.Drawing.Size(567, 136);
            this.listAlarms.TabIndex = 4;
            this.listAlarms.SelectedIndexChanged += new System.EventHandler(this.listAlarms_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(4, 209);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 17);
            this.label5.TabIndex = 30;
            this.label5.Text = "Comment";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnRefreshAlarms
            // 
            this.btnRefreshAlarms.Enabled = false;
            this.btnRefreshAlarms.Location = new System.Drawing.Point(395, 23);
            this.btnRefreshAlarms.Name = "btnRefreshAlarms";
            this.btnRefreshAlarms.Size = new System.Drawing.Size(81, 21);
            this.btnRefreshAlarms.TabIndex = 34;
            this.btnRefreshAlarms.Text = "Refresh";
            this.btnRefreshAlarms.Click += new System.EventHandler(this.btnRefreshAlarms_Click);
            // 
            // tbAlarmComment
            // 
            this.tbAlarmComment.Location = new System.Drawing.Point(78, 208);
            this.tbAlarmComment.Name = "tbAlarmComment";
            this.tbAlarmComment.Size = new System.Drawing.Size(269, 21);
            this.tbAlarmComment.TabIndex = 30;
            // 
            // cbAlarmFalsePositive
            // 
            this.cbAlarmFalsePositive.Location = new System.Drawing.Point(354, 210);
            this.cbAlarmFalsePositive.Name = "cbAlarmFalsePositive";
            this.cbAlarmFalsePositive.Size = new System.Drawing.Size(125, 17);
            this.cbAlarmFalsePositive.TabIndex = 30;
            this.cbAlarmFalsePositive.Text = "False Positive";
            // 
            // btnMarkAlarm
            // 
            this.btnMarkAlarm.Enabled = false;
            this.btnMarkAlarm.Location = new System.Drawing.Point(488, 209);
            this.btnMarkAlarm.Name = "btnMarkAlarm";
            this.btnMarkAlarm.Size = new System.Drawing.Size(84, 21);
            this.btnMarkAlarm.TabIndex = 30;
            this.btnMarkAlarm.Text = "Mark Alarm";
            this.btnMarkAlarm.Click += new System.EventHandler(this.btnMarkAlarm_Click);
            // 
            // gbDevice
            // 
            this.gbDevice.Controls.Add(this.lvDevices);
            this.gbDevice.Controls.Add(this.btnGetDevices);
            this.gbDevice.Controls.Add(this.btnGetServers);
            this.gbDevice.Controls.Add(this.cbServers);
            this.gbDevice.Location = new System.Drawing.Point(251, 3);
            this.gbDevice.Name = "gbDevice";
            this.gbDevice.Size = new System.Drawing.Size(411, 175);
            this.gbDevice.TabIndex = 41;
            this.gbDevice.TabStop = false;
            this.gbDevice.Text = "&Device";
            // 
            // lvDevices
            // 
            this.lvDevices.AllowColumnReorder = true;
            this.lvDevices.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colId,
            this.colName});
            this.lvDevices.FullRowSelect = true;
            this.lvDevices.Location = new System.Drawing.Point(16, 52);
            this.lvDevices.MultiSelect = false;
            this.lvDevices.Name = "lvDevices";
            this.lvDevices.Size = new System.Drawing.Size(380, 114);
            this.lvDevices.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvDevices.TabIndex = 11;
            this.lvDevices.UseCompatibleStateImageBehavior = false;
            this.lvDevices.View = System.Windows.Forms.View.Details;
            this.lvDevices.SelectedIndexChanged += new System.EventHandler(this.lvDevices_SelectedIndexChanged);
            // 
            // colId
            // 
            this.colId.Text = "ID";
            this.colId.Width = 64;
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 227;
            // 
            // btnGetDevices
            // 
            this.btnGetDevices.Enabled = false;
            this.btnGetDevices.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnGetDevices.Location = new System.Drawing.Point(306, 23);
            this.btnGetDevices.Name = "btnGetDevices";
            this.btnGetDevices.Size = new System.Drawing.Size(90, 24);
            this.btnGetDevices.TabIndex = 10;
            this.btnGetDevices.Text = "Get Devices";
            this.btnGetDevices.UseVisualStyleBackColor = true;
            this.btnGetDevices.Click += new System.EventHandler(this.btnGetDevices_Click);
            // 
            // btnGetServers
            // 
            this.btnGetServers.Enabled = false;
            this.btnGetServers.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnGetServers.Location = new System.Drawing.Point(14, 23);
            this.btnGetServers.Name = "btnGetServers";
            this.btnGetServers.Size = new System.Drawing.Size(90, 24);
            this.btnGetServers.TabIndex = 9;
            this.btnGetServers.Text = "Get Servers";
            this.btnGetServers.UseVisualStyleBackColor = true;
            this.btnGetServers.Click += new System.EventHandler(this.btnGetServers_Click);
            // 
            // cbServers
            // 
            this.cbServers.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cbServers.FormattingEnabled = true;
            this.cbServers.Location = new System.Drawing.Point(104, 24);
            this.cbServers.Name = "cbServers";
            this.cbServers.Size = new System.Drawing.Size(202, 20);
            this.cbServers.TabIndex = 7;
            this.cbServers.SelectedIndexChanged += new System.EventHandler(this.cbServers_SelectedIndexChanged);
            // 
            // gbLogin
            // 
            this.gbLogin.Controls.Add(this.btnDisconnect);
            this.gbLogin.Controls.Add(this.lblUser);
            this.gbLogin.Controls.Add(this.tbUser);
            this.gbLogin.Controls.Add(this.btnConnect);
            this.gbLogin.Controls.Add(this.lblPass);
            this.gbLogin.Controls.Add(this.tbIp);
            this.gbLogin.Controls.Add(this.tbPass);
            this.gbLogin.Controls.Add(this.lblIp);
            this.gbLogin.Location = new System.Drawing.Point(4, 3);
            this.gbLogin.Name = "gbLogin";
            this.gbLogin.Size = new System.Drawing.Size(240, 175);
            this.gbLogin.TabIndex = 40;
            this.gbLogin.TabStop = false;
            this.gbLogin.Text = "&AVMS Server";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(126, 127);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(90, 22);
            this.btnDisconnect.TabIndex = 30;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // lblUser
            // 
            this.lblUser.Location = new System.Drawing.Point(13, 65);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(77, 17);
            this.lblUser.TabIndex = 0;
            this.lblUser.Text = "Username";
            this.lblUser.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbUser
            // 
            this.tbUser.Location = new System.Drawing.Point(97, 64);
            this.tbUser.Name = "tbUser";
            this.tbUser.Size = new System.Drawing.Size(120, 21);
            this.tbUser.TabIndex = 2;
            this.tbUser.Text = "admin";
            // 
            // btnConnect
            // 
            this.btnConnect.Enabled = false;
            this.btnConnect.Location = new System.Drawing.Point(28, 127);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(94, 22);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Connect";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // lblPass
            // 
            this.lblPass.Location = new System.Drawing.Point(13, 93);
            this.lblPass.Name = "lblPass";
            this.lblPass.Size = new System.Drawing.Size(77, 17);
            this.lblPass.TabIndex = 2;
            this.lblPass.Text = "Password";
            this.lblPass.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbIp
            // 
            this.tbIp.Location = new System.Drawing.Point(97, 36);
            this.tbIp.Name = "tbIp";
            this.tbIp.Size = new System.Drawing.Size(120, 21);
            this.tbIp.TabIndex = 1;
            this.tbIp.Text = "127.0.0.1";
            // 
            // tbPass
            // 
            this.tbPass.Location = new System.Drawing.Point(97, 92);
            this.tbPass.Name = "tbPass";
            this.tbPass.PasswordChar = '*';
            this.tbPass.Size = new System.Drawing.Size(120, 21);
            this.tbPass.TabIndex = 3;
            this.tbPass.Text = "admin";
            this.tbPass.UseSystemPasswordChar = true;
            // 
            // lblIp
            // 
            this.lblIp.Location = new System.Drawing.Point(23, 37);
            this.lblIp.Name = "lblIp";
            this.lblIp.Size = new System.Drawing.Size(67, 17);
            this.lblIp.TabIndex = 4;
            this.lblIp.Text = "Server IP";
            this.lblIp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // gbInsertAlarm
            // 
            this.gbInsertAlarm.Controls.Add(this.tbCameraID);
            this.gbInsertAlarm.Controls.Add(this.lblCameraID);
            this.gbInsertAlarm.Controls.Add(this.btnInsertAlarm);
            this.gbInsertAlarm.Controls.Add(this.tbAlarmText2);
            this.gbInsertAlarm.Controls.Add(this.nudPolicyID);
            this.gbInsertAlarm.Controls.Add(this.tbAlarmText1);
            this.gbInsertAlarm.Controls.Add(this.lblAlarmText2);
            this.gbInsertAlarm.Controls.Add(this.lblAlarmText1);
            this.gbInsertAlarm.Controls.Add(this.lblPolicyID);
            this.gbInsertAlarm.Controls.Add(this.cbCurrentTime);
            this.gbInsertAlarm.Controls.Add(this.tbUTCTime);
            this.gbInsertAlarm.Controls.Add(this.lblAlarmTime);
            this.gbInsertAlarm.Location = new System.Drawing.Point(4, 184);
            this.gbInsertAlarm.Name = "gbInsertAlarm";
            this.gbInsertAlarm.Size = new System.Drawing.Size(236, 237);
            this.gbInsertAlarm.TabIndex = 39;
            this.gbInsertAlarm.TabStop = false;
            this.gbInsertAlarm.Text = "&Insert Alarm";
            // 
            // tbCameraID
            // 
            this.tbCameraID.Location = new System.Drawing.Point(96, 76);
            this.tbCameraID.Name = "tbCameraID";
            this.tbCameraID.Size = new System.Drawing.Size(120, 21);
            this.tbCameraID.TabIndex = 52;
            this.tbCameraID.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbCameraID_KeyDown);
            // 
            // lblCameraID
            // 
            this.lblCameraID.AutoSize = true;
            this.lblCameraID.Location = new System.Drawing.Point(19, 81);
            this.lblCameraID.Name = "lblCameraID";
            this.lblCameraID.Size = new System.Drawing.Size(59, 12);
            this.lblCameraID.TabIndex = 51;
            this.lblCameraID.Text = "Camera ID";
            // 
            // btnInsertAlarm
            // 
            this.btnInsertAlarm.Enabled = false;
            this.btnInsertAlarm.Location = new System.Drawing.Point(35, 200);
            this.btnInsertAlarm.Name = "btnInsertAlarm";
            this.btnInsertAlarm.Size = new System.Drawing.Size(122, 25);
            this.btnInsertAlarm.TabIndex = 9;
            this.btnInsertAlarm.Text = "Insert One Alarm";
            this.btnInsertAlarm.UseVisualStyleBackColor = true;
            this.btnInsertAlarm.Click += new System.EventHandler(this.btnInsertAlarm_Click);
            // 
            // tbAlarmText2
            // 
            this.tbAlarmText2.Location = new System.Drawing.Point(96, 158);
            this.tbAlarmText2.Name = "tbAlarmText2";
            this.tbAlarmText2.Size = new System.Drawing.Size(120, 21);
            this.tbAlarmText2.TabIndex = 8;
            this.tbAlarmText2.Text = "Added from SDK";
            // 
            // nudPolicyID
            // 
            this.nudPolicyID.Location = new System.Drawing.Point(96, 103);
            this.nudPolicyID.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudPolicyID.Name = "nudPolicyID";
            this.nudPolicyID.Size = new System.Drawing.Size(74, 21);
            this.nudPolicyID.TabIndex = 36;
            // 
            // tbAlarmText1
            // 
            this.tbAlarmText1.Location = new System.Drawing.Point(96, 131);
            this.tbAlarmText1.Name = "tbAlarmText1";
            this.tbAlarmText1.Size = new System.Drawing.Size(120, 21);
            this.tbAlarmText1.TabIndex = 7;
            this.tbAlarmText1.Text = "Custom Alarm";
            // 
            // lblAlarmText2
            // 
            this.lblAlarmText2.AutoSize = true;
            this.lblAlarmText2.Location = new System.Drawing.Point(8, 162);
            this.lblAlarmText2.Name = "lblAlarmText2";
            this.lblAlarmText2.Size = new System.Drawing.Size(77, 12);
            this.lblAlarmText2.TabIndex = 5;
            this.lblAlarmText2.Text = "Alarm Text 2";
            // 
            // lblAlarmText1
            // 
            this.lblAlarmText1.AutoSize = true;
            this.lblAlarmText1.Location = new System.Drawing.Point(10, 135);
            this.lblAlarmText1.Name = "lblAlarmText1";
            this.lblAlarmText1.Size = new System.Drawing.Size(77, 12);
            this.lblAlarmText1.TabIndex = 4;
            this.lblAlarmText1.Text = "Alarm Text 1";
            // 
            // lblPolicyID
            // 
            this.lblPolicyID.AutoSize = true;
            this.lblPolicyID.Location = new System.Drawing.Point(29, 107);
            this.lblPolicyID.Name = "lblPolicyID";
            this.lblPolicyID.Size = new System.Drawing.Size(59, 12);
            this.lblPolicyID.TabIndex = 3;
            this.lblPolicyID.Text = "Policy ID";
            // 
            // cbCurrentTime
            // 
            this.cbCurrentTime.AutoSize = true;
            this.cbCurrentTime.Checked = true;
            this.cbCurrentTime.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbCurrentTime.Location = new System.Drawing.Point(96, 28);
            this.cbCurrentTime.Name = "cbCurrentTime";
            this.cbCurrentTime.Size = new System.Drawing.Size(120, 16);
            this.cbCurrentTime.TabIndex = 2;
            this.cbCurrentTime.Text = "Use Current Time";
            this.cbCurrentTime.UseVisualStyleBackColor = true;
            this.cbCurrentTime.CheckedChanged += new System.EventHandler(this.cbCurrentTime_CheckedChanged);
            // 
            // tbUTCTime
            // 
            this.tbUTCTime.Location = new System.Drawing.Point(96, 48);
            this.tbUTCTime.Name = "tbUTCTime";
            this.tbUTCTime.Size = new System.Drawing.Size(120, 21);
            this.tbUTCTime.TabIndex = 50;
            this.tbUTCTime.Text = "0";
            // 
            // lblAlarmTime
            // 
            this.lblAlarmTime.AutoSize = true;
            this.lblAlarmTime.Location = new System.Drawing.Point(17, 52);
            this.lblAlarmTime.Name = "lblAlarmTime";
            this.lblAlarmTime.Size = new System.Drawing.Size(65, 12);
            this.lblAlarmTime.TabIndex = 0;
            this.lblAlarmTime.Text = "Alarm Time";
            // 
            // tbAlarmMessage
            // 
            this.tbAlarmMessage.Location = new System.Drawing.Point(17, 34);
            this.tbAlarmMessage.Name = "tbAlarmMessage";
            this.tbAlarmMessage.Size = new System.Drawing.Size(643, 21);
            this.tbAlarmMessage.TabIndex = 32;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnFilter);
            this.panel2.Controls.Add(this.btnProcessAlarm);
            this.panel2.Controls.Add(this.tbPolicyType);
            this.panel2.Controls.Add(this.lblPolicyType);
            this.panel2.Controls.Add(this.btnParseAlarm);
            this.panel2.Controls.Add(this.lblRuleAction);
            this.panel2.Controls.Add(this.listRuleAction);
            this.panel2.Controls.Add(this.tbRuleAction);
            this.panel2.Controls.Add(this.tbAlarmType);
            this.panel2.Controls.Add(this.lblAlarmType);
            this.panel2.Controls.Add(this.label17);
            this.panel2.Controls.Add(this.tbAlarmMessage);
            this.panel2.Location = new System.Drawing.Point(4, 438);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(734, 234);
            this.panel2.TabIndex = 3;
            // 
            // btnFilter
            // 
            this.btnFilter.Location = new System.Drawing.Point(17, 110);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(119, 25);
            this.btnFilter.TabIndex = 48;
            this.btnFilter.Text = "Filter";
            this.btnFilter.UseVisualStyleBackColor = true;
            this.btnFilter.Click += new System.EventHandler(this.btnFilter_Click);
            // 
            // btnProcessAlarm
            // 
            this.btnProcessAlarm.Location = new System.Drawing.Point(17, 85);
            this.btnProcessAlarm.Name = "btnProcessAlarm";
            this.btnProcessAlarm.Size = new System.Drawing.Size(119, 25);
            this.btnProcessAlarm.TabIndex = 47;
            this.btnProcessAlarm.Text = "Process";
            this.btnProcessAlarm.UseVisualStyleBackColor = true;
            this.btnProcessAlarm.Click += new System.EventHandler(this.btnProcessAlarm_Click);
            // 
            // tbPolicyType
            // 
            this.tbPolicyType.Location = new System.Drawing.Point(143, 85);
            this.tbPolicyType.Name = "tbPolicyType";
            this.tbPolicyType.Size = new System.Drawing.Size(120, 21);
            this.tbPolicyType.TabIndex = 46;
            // 
            // lblPolicyType
            // 
            this.lblPolicyType.AutoSize = true;
            this.lblPolicyType.Location = new System.Drawing.Point(167, 65);
            this.lblPolicyType.Name = "lblPolicyType";
            this.lblPolicyType.Size = new System.Drawing.Size(71, 12);
            this.lblPolicyType.TabIndex = 45;
            this.lblPolicyType.Text = "Policy Type";
            // 
            // btnParseAlarm
            // 
            this.btnParseAlarm.Location = new System.Drawing.Point(17, 60);
            this.btnParseAlarm.Name = "btnParseAlarm";
            this.btnParseAlarm.Size = new System.Drawing.Size(119, 25);
            this.btnParseAlarm.TabIndex = 43;
            this.btnParseAlarm.Text = "Parse";
            this.btnParseAlarm.UseVisualStyleBackColor = true;
            this.btnParseAlarm.Click += new System.EventHandler(this.btnParseAlarm_Click);
            // 
            // lblRuleAction
            // 
            this.lblRuleAction.AutoSize = true;
            this.lblRuleAction.Location = new System.Drawing.Point(20, 142);
            this.lblRuleAction.Name = "lblRuleAction";
            this.lblRuleAction.Size = new System.Drawing.Size(71, 12);
            this.lblRuleAction.TabIndex = 42;
            this.lblRuleAction.Text = "Rule Action";
            // 
            // listRuleAction
            // 
            this.listRuleAction.FormattingEnabled = true;
            this.listRuleAction.HorizontalScrollbar = true;
            this.listRuleAction.ItemHeight = 12;
            this.listRuleAction.Location = new System.Drawing.Point(144, 111);
            this.listRuleAction.Name = "listRuleAction";
            this.listRuleAction.Size = new System.Drawing.Size(583, 100);
            this.listRuleAction.TabIndex = 41;
            this.listRuleAction.SelectedIndexChanged += new System.EventHandler(this.listRuleAction_SelectedIndexChanged);
            // 
            // tbRuleAction
            // 
            this.tbRuleAction.Location = new System.Drawing.Point(17, 162);
            this.tbRuleAction.Multiline = true;
            this.tbRuleAction.Name = "tbRuleAction";
            this.tbRuleAction.ReadOnly = true;
            this.tbRuleAction.Size = new System.Drawing.Size(119, 21);
            this.tbRuleAction.TabIndex = 40;
            // 
            // tbAlarmType
            // 
            this.tbAlarmType.Location = new System.Drawing.Point(270, 85);
            this.tbAlarmType.Name = "tbAlarmType";
            this.tbAlarmType.Size = new System.Drawing.Size(120, 21);
            this.tbAlarmType.TabIndex = 38;
            // 
            // lblAlarmType
            // 
            this.lblAlarmType.AutoSize = true;
            this.lblAlarmType.Location = new System.Drawing.Point(292, 65);
            this.lblAlarmType.Name = "lblAlarmType";
            this.lblAlarmType.Size = new System.Drawing.Size(65, 12);
            this.lblAlarmType.TabIndex = 37;
            this.lblAlarmType.Text = "Alarm Type";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(17, 13);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(425, 12);
            this.label17.TabIndex = 36;
            this.label17.Text = "AlarmId | AlarmTime | PolicyType | AlarmType |  CameraId | CameraName ";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.gbLogin);
            this.panel3.Controls.Add(this.gbReceiveAlarm);
            this.panel3.Controls.Add(this.gbDevice);
            this.panel3.Controls.Add(this.gbInsertAlarm);
            this.panel3.Location = new System.Drawing.Point(605, 2);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(835, 430);
            this.panel3.TabIndex = 5;
            // 
            // gbAVMSFuntion
            // 
            this.gbAVMSFuntion.Controls.Add(this.checkRecording);
            this.gbAVMSFuntion.Controls.Add(this.btnStartRecording);
            this.gbAVMSFuntion.Controls.Add(this.btnStopRecording);
            this.gbAVMSFuntion.Controls.Add(this.checkPopup);
            this.gbAVMSFuntion.Controls.Add(this.bnResumeRecording);
            this.gbAVMSFuntion.Controls.Add(this.btnSnapshot);
            this.gbAVMSFuntion.Location = new System.Drawing.Point(17, 15);
            this.gbAVMSFuntion.Name = "gbAVMSFuntion";
            this.gbAVMSFuntion.Size = new System.Drawing.Size(663, 88);
            this.gbAVMSFuntion.TabIndex = 46;
            this.gbAVMSFuntion.TabStop = false;
            this.gbAVMSFuntion.Text = "&AVMS Funtion";
            // 
            // checkRecording
            // 
            this.checkRecording.AutoSize = true;
            this.checkRecording.Location = new System.Drawing.Point(10, 28);
            this.checkRecording.Name = "checkRecording";
            this.checkRecording.Size = new System.Drawing.Size(78, 16);
            this.checkRecording.TabIndex = 40;
            this.checkRecording.Text = "Recording";
            this.checkRecording.UseVisualStyleBackColor = true;
            this.checkRecording.CheckedChanged += new System.EventHandler(this.cbRecording_CheckedChanged);
            // 
            // btnStartRecording
            // 
            this.btnStartRecording.Location = new System.Drawing.Point(104, 26);
            this.btnStartRecording.Name = "btnStartRecording";
            this.btnStartRecording.Size = new System.Drawing.Size(58, 21);
            this.btnStartRecording.TabIndex = 33;
            this.btnStartRecording.Text = "Start";
            this.btnStartRecording.Click += new System.EventHandler(this.bnStartRecording_Click);
            // 
            // btnStopRecording
            // 
            this.btnStopRecording.Location = new System.Drawing.Point(163, 26);
            this.btnStopRecording.Name = "btnStopRecording";
            this.btnStopRecording.Size = new System.Drawing.Size(58, 21);
            this.btnStopRecording.TabIndex = 34;
            this.btnStopRecording.Text = "Stop";
            this.btnStopRecording.Click += new System.EventHandler(this.bnStopRecording_Click);
            // 
            // checkPopup
            // 
            this.checkPopup.AutoSize = true;
            this.checkPopup.Location = new System.Drawing.Point(10, 53);
            this.checkPopup.Name = "checkPopup";
            this.checkPopup.Size = new System.Drawing.Size(54, 16);
            this.checkPopup.TabIndex = 39;
            this.checkPopup.Text = "Popup";
            this.checkPopup.UseVisualStyleBackColor = true;
            this.checkPopup.CheckedChanged += new System.EventHandler(this.cbPopup_CheckedChanged);
            // 
            // bnResumeRecording
            // 
            this.bnResumeRecording.Location = new System.Drawing.Point(222, 26);
            this.bnResumeRecording.Name = "bnResumeRecording";
            this.bnResumeRecording.Size = new System.Drawing.Size(72, 21);
            this.bnResumeRecording.TabIndex = 35;
            this.bnResumeRecording.Text = "Resume";
            this.bnResumeRecording.Click += new System.EventHandler(this.bnResumeRecordMode_Click);
            // 
            // btnSnapshot
            // 
            this.btnSnapshot.Location = new System.Drawing.Point(104, 51);
            this.btnSnapshot.Name = "btnSnapshot";
            this.btnSnapshot.Size = new System.Drawing.Size(82, 21);
            this.btnSnapshot.TabIndex = 32;
            this.btnSnapshot.Text = "Snapshot";
            this.btnSnapshot.UseVisualStyleBackColor = true;
            this.btnSnapshot.Click += new System.EventHandler(this.bnJpg_Click);
            // 
            // gbHTTPRequest
            // 
            this.gbHTTPRequest.Controls.Add(this.cbExecuteAction);
            this.gbHTTPRequest.Controls.Add(this.lblResult);
            this.gbHTTPRequest.Controls.Add(this.tbResult);
            this.gbHTTPRequest.Controls.Add(this.btnSend);
            this.gbHTTPRequest.Controls.Add(this.tbBody);
            this.gbHTTPRequest.Controls.Add(this.lblBody);
            this.gbHTTPRequest.Controls.Add(this.tbURL);
            this.gbHTTPRequest.Controls.Add(this.lblURL);
            this.gbHTTPRequest.Location = new System.Drawing.Point(17, 110);
            this.gbHTTPRequest.Name = "gbHTTPRequest";
            this.gbHTTPRequest.Size = new System.Drawing.Size(663, 252);
            this.gbHTTPRequest.TabIndex = 45;
            this.gbHTTPRequest.TabStop = false;
            this.gbHTTPRequest.Text = "&HTTP Request";
            // 
            // cbExecuteAction
            // 
            this.cbExecuteAction.AllowDrop = true;
            this.cbExecuteAction.FormattingEnabled = true;
            this.cbExecuteAction.Location = new System.Drawing.Point(138, 207);
            this.cbExecuteAction.Name = "cbExecuteAction";
            this.cbExecuteAction.Size = new System.Drawing.Size(83, 20);
            this.cbExecuteAction.TabIndex = 7;
            this.cbExecuteAction.SelectedIndexChanged += new System.EventHandler(this.cbExecuteAction_SelectedIndexChanged);
            // 
            // lblResult
            // 
            this.lblResult.AutoSize = true;
            this.lblResult.Location = new System.Drawing.Point(318, 17);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(41, 12);
            this.lblResult.TabIndex = 6;
            this.lblResult.Text = "Result";
            // 
            // tbResult
            // 
            this.tbResult.Location = new System.Drawing.Point(322, 39);
            this.tbResult.Multiline = true;
            this.tbResult.Name = "tbResult";
            this.tbResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbResult.Size = new System.Drawing.Size(334, 200);
            this.tbResult.TabIndex = 5;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(224, 206);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(90, 24);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // tbBody
            // 
            this.tbBody.Location = new System.Drawing.Point(14, 101);
            this.tbBody.Multiline = true;
            this.tbBody.Name = "tbBody";
            this.tbBody.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbBody.Size = new System.Drawing.Size(300, 100);
            this.tbBody.TabIndex = 3;
            // 
            // lblBody
            // 
            this.lblBody.AutoSize = true;
            this.lblBody.Location = new System.Drawing.Point(20, 83);
            this.lblBody.Name = "lblBody";
            this.lblBody.Size = new System.Drawing.Size(29, 12);
            this.lblBody.TabIndex = 2;
            this.lblBody.Text = "Body";
            // 
            // tbURL
            // 
            this.tbURL.Location = new System.Drawing.Point(12, 48);
            this.tbURL.Multiline = true;
            this.tbURL.Name = "tbURL";
            this.tbURL.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbURL.Size = new System.Drawing.Size(302, 32);
            this.tbURL.TabIndex = 1;
            // 
            // lblURL
            // 
            this.lblURL.AutoSize = true;
            this.lblURL.Location = new System.Drawing.Point(18, 30);
            this.lblURL.Name = "lblURL";
            this.lblURL.Size = new System.Drawing.Size(23, 12);
            this.lblURL.TabIndex = 0;
            this.lblURL.Text = "URL";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripStatusLabel3});
            this.statusStrip1.Location = new System.Drawing.Point(0, 753);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1203, 22);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(1188, 17);
            this.toolStripStatusLabel2.Spring = true;
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(0, 17);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.time1_Tick);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.gbHTTPRequest);
            this.panel4.Controls.Add(this.gbAVMSFuntion);
            this.panel4.Location = new System.Drawing.Point(745, 438);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(695, 372);
            this.panel4.TabIndex = 7;
            // 
            // timer2
            // 
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(1203, 775);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Transaction Plugin";
            this.panel1.ResumeLayout(false);
            this.gbConfiguration.ResumeLayout(false);
            this.gbConfiguration.PerformLayout();
            this.gbJobs.ResumeLayout(false);
            this.gbReceiveAlarm.ResumeLayout(false);
            this.gbReceiveAlarm.PerformLayout();
            this.gbDevice.ResumeLayout(false);
            this.gbLogin.ResumeLayout(false);
            this.gbLogin.PerformLayout();
            this.gbInsertAlarm.ResumeLayout(false);
            this.gbInsertAlarm.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPolicyID)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.gbAVMSFuntion.ResumeLayout(false);
            this.gbAVMSFuntion.PerformLayout();
            this.gbHTTPRequest.ResumeLayout(false);
            this.gbHTTPRequest.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.Run(new Form1(args));
        }

        public static uint GetAddr(string address)
        {
            IPAddress ip = IPAddress.Parse(address);
            Byte[] bytes = ip.GetAddressBytes();
            int addr;

            addr = (bytes[0] << 24) |
                (bytes[1] << 16) |
                (bytes[2] << 8) |
                (bytes[3]);
            return (uint)addr;
        }


        //private void SetConnectButton(bool bEnable)
        //{
        //    if (InvokeRequired)
        //    {
        //        Invoke(new DelegateBool(SetConnectButton), new object[] { bEnable });
        //        return;
        //    }

        //    btnConnect.Enabled = (!m_bConnectedToAVMSServer) && bEnable;
        //    btnDisconnect.Enabled = m_bConnectedToAVMSServer && (!bEnable);
        //}
        private void SetConnectButton(bool bEnable, bool bConnected)
        {
            if (InvokeRequired)
            {
                Invoke(new DelegateBool2(SetConnectButton), new object[] { bEnable, bConnected });
                return;
            }

            btnConnect.Enabled = (!bConnected) && bEnable;
            btnDisconnect.Enabled = bConnected && (!bEnable);
        }

        //private void UpdateControls()
        //{
        //    if (InvokeRequired)
        //    {
        //        Invoke(new DelegateVoid(UpdateControls));
        //        return;
        //    }

        //    btnGetServers.Enabled = m_bConnectedToAVMSServer;
        //    btnGetDevices.Enabled = m_bConnectedToAVMSServer;
        //    btnListen.Enabled = m_bConnectedToAVMSServer;
        //    btnRefreshAlarms.Enabled = m_bConnectedToAVMSServer;
        //    btnInsertAlarm.Enabled = m_bConnectedToAVMSServer;
        //    btnMarkAlarm.Enabled = m_bConnectedToAVMSServer;

        //    btnStartRecording.Enabled = m_bConnectedToAVMSServer;
        //    btnStopRecording.Enabled = m_bConnectedToAVMSServer;
        //    bnResumeRecording.Enabled = m_bConnectedToAVMSServer;
        //    btnSnapshot.Enabled = m_bConnectedToAVMSServer;
        //}
        private void UpdateControls(bool bEnable)
        {
            if (InvokeRequired)
            {
                Invoke(new DelegateBool(UpdateControls), new object[] { bEnable });
                return;
            }

            btnGetServers.Enabled = bEnable;
            btnGetDevices.Enabled = bEnable;
            btnListen.Enabled = bEnable;
            btnRefreshAlarms.Enabled = bEnable;
            btnInsertAlarm.Enabled = bEnable;
            btnMarkAlarm.Enabled = bEnable;

            btnStartRecording.Enabled = bEnable;
            btnStopRecording.Enabled = bEnable;
            bnResumeRecording.Enabled = bEnable;
            btnSnapshot.Enabled = bEnable;
        }

        private void UpdateControl(Control obj, bool enabled)
        {
            if (InvokeRequired)
            {
                Invoke(new DelegateVoid2(UpdateControl), new object[] { obj, enabled });
                return;
            }

            obj.Enabled = enabled;
        }

        private void ClearAlarms()
        {
            if (InvokeRequired)
            {
                Invoke(new DelegateVoid(ClearAlarms));
                return;
            }

            listAlarms.Items.Clear();
            listAlarms.Invalidate();
        }

        private void ClearDevices()
        {
            if (InvokeRequired)
            {
                Invoke(new DelegateVoid(ClearDevices));
                return;
            }

            lvDevices.Items.Clear();
            lvDevices.Invalidate();
        }

        private void ClearServers()
        {
            if (InvokeRequired)
            {
                Invoke(new DelegateVoid(ClearServers));
                return;
            }

            cbServers.Items.Clear();
            cbServers.Invalidate();
        }

        private void Release()
        {
            ClearServers();
            ClearDevices();
            ClearAlarms();
        }

        private void SelectDefaultServer()
        {
            if (cbServers.Items.Count > 0)
            {
                SelectServer(0);
            }
            else
            {
                SelectServer(-1);
            }
        }

        private void SelectServer(int id)
        {
            cbServers.SelectedIndex = id;
        }

        private void SelectDefaultDevice()
        {
            if (lvDevices.Items.Count > 0)
            {
                if (null != m_camera)
                {
                    ListViewItem item = lvDevices.FindItemWithText(m_camera.Name);
                    if (null != item)
                    {
                        item.Selected = true;
                        m_iCameraId = (int)m_camera.CameraId;
                    }
                    else
                    {
                        SelectDevice(0);
                    }
                }
                else
                {
                    SelectDevice(0);
                }
            }
            else
            {
                SelectDevice(-1);
            }
        }

        private void SelectDevice(int id)
        {
            if (id < 0)
            {
                return;
            }
            lvDevices.Items[id].Selected = true;
            lvDevices.Focus();  // focus one item (will lose if not selected)
            lvDevices.Columns[1].Width = -1;    // -1 / -2 (used in NotificationMonitorForm's SetLogColumnWidth)
        }

        private string RefreshDeviceManager()
        {
            try
            {
                if (null == m_deviceManager)
                {
                    return "Failed to access Device Manager ： Value null";
                }

                AddDeviceModelEventHandler(m_deviceManager, ref m_bDeviceModelEventHandlerAdded);
                m_deviceManager.Refresh();
            }
            catch (Exception ex)
            {
                return "Failed to refresh Device Manager : " + ex.ToString();
            }

            return string.Empty;
        }

        private void SelectCamera(CCamera cam)
        {
            if (null == cam)
            {
                m_camera = null;
                m_iCameraId = -1;
                return;
            }
            m_camera = cam;
            m_iCameraId = (int)cam.CameraId;
        }

        private bool SelectCameraById(uint id)
        {
            if ((null == m_camera) || (id != m_camera.CameraId))
            {
                if (null != m_deviceManager)
                {
                    CCamera cam = m_deviceManager.GetCameraById(id);
                    if (null == cam)
                    {
                        return false;
                    }

                    SelectCamera(cam);
                }
            }

            return true;
        }

        private void PopulateServerList(string[] servers)
        {
            m_bServerListPopulating = true;

            cbServers.Items.Clear();
            foreach (string server in servers)
            {
                cbServers.Items.Add(server);
            }

            m_bServerListPopulating = false;
        }

        private void PopulateCameraList(List<CCamera> cameras)
        {
            //m_bCameraListPopulating = true;

            lvDevices.Items.Clear();
            foreach (CCamera cam in cameras)
            {
                ListViewItem item = new ListViewItem(new string[] { cam.CameraId.ToString(), cam.ToString() });
                lvDevices.Items.Add(item);
            }

            //m_bCameraListPopulating = false;
        }

        private void MarkAlarm()
        {
            if (null == m_avms)
            {
                MessageBox.Show("Fail to connect to AVMS!");
                return;
            }

            string log = (string)listAlarms.SelectedItem;
            if (null != log)
            {
                string[] logArray = log.Split('|');
                uint uiAlarmId = 0;
                uint.TryParse(logArray[0], out uiAlarmId);

                DataSet ret = null;
                m_avms.MarkAlarm(m_camera, uiAlarmId, cbAlarmFalsePositive.Checked, tbAlarmComment.Text, ref ret);
                if ((null != ret) && (ret.Tables.Count > 0))
                {
                    DataTable dt = ret.Tables[0];
                    int iRows = dt.Rows.Count;
                    if (1 == iRows)
                    {
                        CameraLogStruct cms = MakeRecord(dt.Rows[0]);
                        log = MakeAlarmLog(cms);
                        ModifyAlarmLog(log, listAlarms.SelectedIndex);
                    }
                }
            }
        }

        private void RefreshAlarms()
        {
            if ((null == m_camera) && (string.Empty != tbCameraID.Text))
            {
                uint uiCameraId = uint.Parse(tbCameraID.Text);
                if (!SelectCameraById(uiCameraId))
                {
                    return;
                }
            }

            listAlarms.Items.Clear();
            DateTime dtStart = DateTime.Parse(dtStartTime.Text);
            DateTime dtStop = DateTime.Parse(dtStopTime.Text);
            GetAlarmsFromWS(dtStart, dtStop.AddDays(1));
        }

        public void GetAlarmsFromWS(DateTime dtStartGMT, DateTime dtEndGMT)
        {
            if (null == m_avms)
            {
                MessageBox.Show("Fail to connect to AVMS!");
                return;
            }

            if (!m_farm.CanAccess(FarmRight.ViewAlarm) || dtStartGMT >= dtEndGMT)
            {
                MessageBox.Show("Fail to get alarms!");
                return;
            }

            byte[] result = null;
            bool isExported = m_avms.ExportSignalsStream(m_camera, dtStartGMT, dtEndGMT, ref result);
            if (!isExported)
            {
                return;
            }

            DataSet ret = null;
            switch (result[0])
            {
                case 1:
                    MemoryStream compStream = new MemoryStream(result, 1, result.Length - 1, false);
                    GZipInputStream uncompStream = new GZipInputStream(compStream);

                    ret = new DataSet();
                    ret.ReadXml(uncompStream);
                    break;

                default:
                    MessageBox.Show("Unexpected version retreiving alarm list from server " + m_camera.Server.Name);
                    return;
            }

            // populate the control
            if ((null != ret) && (ret.Tables.Count > 0))
            {
                DataTable dt = ret.Tables[0];
                int iRows = dt.Rows.Count;
                for (int i = 0; i < iRows; i++)
                {
                    DataRow dr = dt.Rows[i];
                    CameraLogStruct cms = MakeRecord(dr);
                    string log = MakeAlarmLog(cms);
                    AddAlarmLog(log);
                }

                //listAlarms.SelectedIndex = listAlarms.Items.Count - 1;  // not focus when reading historical records
            }
        }

        public CameraLogStruct MakeRecord(DataRow dr)
        {
            CameraLogStruct cms = new CameraLogStruct();
            cms.m_iCameraId = uint.Parse(dr["CameraId"].ToString(), CultureInfo.InvariantCulture);
            cms.m_iState = (System.UInt32)T_ALARM_STATES.WAIT_TILL_STABLE;
            cms.m_iEvent = uint.Parse(dr["AlarmTypeId"].ToString(), CultureInfo.InvariantCulture);
            cms.m_milliTime = ushort.Parse(dr["TmAlarmMs"].ToString(), CultureInfo.InvariantCulture);
            cms.m_utcTime = uint.Parse(dr["TmAlarm"].ToString(), CultureInfo.InvariantCulture);
            cms.m_iAlarmDbId = uint.Parse(dr["Id"].ToString(), CultureInfo.InvariantCulture);
            // optional items (may not appear if null value)
            if (dr.Table.Columns.Contains("PolicyId"))
            {
                cms.m_iPolicyId = int.Parse(dr["PolicyId"].ToString(), CultureInfo.InvariantCulture);
            }
            else
            {
                cms.m_iPolicyId = 0;
            }
            if (dr.Table.Columns.Contains("MsSinceChange"))
            {
                uint val = 0;
                if (uint.TryParse(dr["MsSinceChange"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out val))
                {
                    cms.m_milliSinceChangeBegan = val;
                }
                else
                {
                    cms.m_milliSinceChangeBegan = 0;
                }
            }
            else
            {
                cms.m_milliSinceChangeBegan = 0;
            }
            if (dr.Table.Columns.Contains("isFalsePositive"))
            {
                cms.m_strConfirm = (dr["isFalsePositive"].ToString() != string.Empty) ? ((dr["isFalsePositive"].ToString() == "0") ? "Real Alarm" : "False Alarm") : "Not Confirmed";
            }
            else
            {
                cms.m_strConfirm = "Not Confirmed";
            }
            if (dr.Table.Columns.Contains("Comment"))
            {
                cms.m_strComment = (dr["Comment"].ToString() != null) ? dr["Comment"].ToString() : string.Empty;
            }
            else
            {
                cms.m_strComment = string.Empty;
            }
            return cms;
        }

        public string MakeAlarmLog(CameraLogStruct cms)
        {
            string log = string.Empty;

            // Calculate the timezone time
            DateTime clientTime = TimeUtils.DateTimeFromUTC(cms.m_utcTime);
            DateTime serverTime = m_camera.Server.ToLocalTime(clientTime);
            cms.m_timezoneTime = (short)(clientTime - serverTime).TotalMinutes;

            CDeviceBaseClient device = null;
            bool bCameraExists = true;

            CCamera camera = m_farm.DeviceManager.GetCameraById(cms.m_iCameraId);
            string deviceName;
            if (camera != null)
            {
                device = camera;
            }
            else
            {
                device = m_farm.DeviceManager.GetAccessDevice(cms.m_iCameraId);
                if (device == null)
                {
                    bCameraExists = false;
                }
            }

            if (bCameraExists)
            {
                deviceName = device.Name;
            }
            else
                deviceName = "Camera not present";

            //CameraMessage cm = new CameraMessage(cms, deviceName);
            //AddAlarm(cm);
            // custom log
            string strAlarmId = cms.m_iAlarmDbId.ToString();
            string strAlarmTime = serverTime.ToString();    // clientTime
            string strCameraId = cms.m_iCameraId.ToString();
            string strPolicyId = cms.m_iPolicyId.ToString();
            string strAlarmTypeId = cms.m_iEvent.ToString();
            //
            string strAlarmConfirm = cms.m_strConfirm;
            string strComment = cms.m_strComment;

            log = String.Format("{0} | {1} | {2} | {3} | {4} | {5} -----> {6}[{7}]", 
                strAlarmId.PadLeft(4 - strAlarmId.Length, ' '), 
                strAlarmTime.PadLeft(20 - strAlarmTime.Length, ' '), 
                strPolicyId.PadLeft(4 - strPolicyId.Length, ' '), 
                strAlarmTypeId.PadLeft(4 - strAlarmTypeId.Length, ' '), 
                strCameraId.PadLeft(4 - strCameraId.Length, ' '), 
                deviceName.PadLeft(30 - deviceName.Length, ' '), 
                strAlarmConfirm, 
                strComment);

            return log;
        }


        private void AddAlarm(CameraMessage cm)
        {
            if (InvokeRequired)
            {
                Invoke(new System.Action<CameraMessage>(AddAlarm), cm);
                return;
            }

            listAlarms.Items.Add(cm);
        }

        private void AddAlarmLog(string log)
        {
            if (InvokeRequired)
            {
                Invoke(new System.Action<string>(AddAlarmLog), log);
                return;
            }

            listAlarms.Items.Add(log);
            //// focus on the latest record
            //listAlarms.SelectedIndex = listAlarms.Items.Count - 1;
            // move to outside for only focusing on the last record
            // but seems not be good solution, now it only focus when append one alarm record
            if (m_bNeedFocus)
            {
                //FocusLastRecord<ListBox>(listAlarms);
                FocusRecord<ListBox>(listAlarms, listAlarms.Items.Count - 1);
            }

            //// if append to the toppest line and no focus, remove above and use Insert
            //listAlarms.Items.Insert(0, log);
        }

        private void ModifyAlarmLog(string log, int index)
        {
            if (InvokeRequired)
            {
                Invoke(new System.Action<string, int>(ModifyAlarmLog), log, index);
                return;
            }

            listAlarms.Items.RemoveAt(index);
            listAlarms.Items.Insert(index, log);
            FocusRecord<ListBox>(listAlarms, index);
        }

        private string InsertAlarm()
        {
            if (null == m_avms)
            {
                return "Fail to connect to AVMS";
            }

            try
            {
                if (string.Empty == tbCameraID.Text)
                {
                    if (null != m_camera)
                    {
                        tbCameraID.Text = m_camera.CameraId.ToString();
                    }
                    else
                    {
                        return "No camera has been selected";
                    }
                }
                else
                {
                    uint uiCameraId = uint.Parse(tbCameraID.Text);
                    if (!SelectCameraById(uiCameraId))
                    {
                        return "Invalid camera";
                    }
                }

                if (cbCurrentTime.Checked)
                {
                    GetUTCTime();
                }
                else
                {
                    if (string.Empty == tbUTCTime.Text)
                    {
                        tbUTCTime.Text = "0";
                    }
                }
                int alarmTime = Int32.Parse(tbUTCTime.Text);

                int policyID = (int)nudPolicyID.Value;
                string alarmText1 = tbAlarmText1.Text;
                string alarmText2 = tbAlarmText2.Text;

                if (m_avms.AddAlarm(m_camera, alarmTime, policyID, alarmText1, alarmText2))
                {
                    return "Alarm added successfully.";
                }
                else
                {
                    return "Alarm could not be added.  Ensure that you have specified the correct camera ID, and that the appropriate service is running (ie 'AI Tracker " + m_camera.CameraId.ToString() + ")";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return "An error occured while inserting alarm: " + ex.ToString();
            }
        }

        private void GetUTCTime()
        {
            tbUTCTime.Text = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        }

        public void Snapshot()
        {
            if (InvokeRequired)
            {
                Invoke(new System.Action(Snapshot));
                return;
            }

            if (null == m_avms)
            {
                MessageBox.Show("Fail to connect to AVMS!");
                return;
            }

            DateTime jpgTime = m_camera.Server.ToUtcTime(DateTime.Now);
            string sFilename = string.Empty;
            byte[] byteJpg = null;
            bool isExported = m_avms.ExportImageStream(m_camera, jpgTime, m_bViewPrivateVideo, ref sFilename, ref byteJpg);
            if (!isExported)
            {
                return;
            }

            if (null != byteJpg)
            {
                MemoryStream ms = new MemoryStream(byteJpg, false);
                Image image = (Bitmap)Image.FromStream(ms);
                FormJpg form = new FormJpg();
                form.pbJpg.Image = image;
                form.Show();
            }
        }

        public void Record()
        {
            if (InvokeRequired)
            {
                Invoke(new System.Action(Record));
                return;
            }

            if (null == m_camera)
                return;
            m_camera.StartRecording();
            m_camera.ResumeRecording();
        }

        private void ConnectToCamera()
        {
            m_bConnectedToAVMSServer = false;
            UpdateControls(m_bConnectedToAVMSServer);
            // if the server isn't connected yet
            if (m_camera.Server.State != CServer.ServerState.Connected)
            {
                m_camera.Server.StateChanged += new EventHandler<Seer.BaseLibCS.ValueChangedEventArgs<CServer.ServerState>>(Server_StateChanged);

                // wait 20 seconds				
                if (!m_waitForServerConnection.WaitOne(TimeSpan.FromSeconds(20)))
                {
                    MessageBox.Show("Failed to connect to server");
                    return;
                }
            }

            m_bConnectedToAVMSServer = true;
            m_bViewPrivateVideo = m_camera.CanAccess(DeviceRight.ViewPrivateVideo);

            UpdateControls(m_bConnectedToAVMSServer);

            _runTimeTimer.Start();
        }

        private void _runTimeTimer_Tick(object sender, EventArgs e)
        {
            DisplayTime();
        }

        private void DisplayTime()
        {
            if (InvokeRequired)
            {
                Invoke(new DelegateVoid(DisplayTime));
                return;
            }

            //long tm;
            //m_dtGMT = new DateTime(tm + dt1970.Ticks);
            //tbTime.Text = m_dtGMT.ToLocalTime().ToString();
            GetUTCTime();
        }



        #endregion

        #region Event Handlers

        private void ServiceJob_EventSend(object sender, JobEventArgs e)
        {
            JobEventHandler handler = new JobEventHandler(JobEvent);
            this.Invoke(handler, new object[] { sender, e });
        }
        public void JobEvent(object sender, JobEventArgs e)
        {
            ServiceJob job = sender as ServiceJob;
            string message = e.Message;

            bool isConnected = job.m_avms.IsConnected;
            string status = string.Format(message);
            Trace.WriteLine(status);
            this.toolStripStatusLabel2.Text = status;
            SetConnectButton(!isConnected, isConnected);
            UpdateControls(isConnected);
        }

        private void Farm_ConnectedEvent(object sender, EventArgs e)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new EventHandler<EventArgs>(Farm_ConnectedEvent), sender, e);
                    return;
                }

                ServiceJob job = sender as ServiceJob;

                bool isConnected = job.m_avms.IsConnected;
                string status = string.Format(job.m_jobId.ToString() + " Connected : avms connection is " + isConnected);
                Trace.WriteLine(status);
                this.toolStripStatusLabel2.Text = status;
                SetConnectButton(!isConnected, isConnected);
                UpdateControls(isConnected);
                job.m_bConnectedToAVMSServer = isConnected;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update staus : " + ex.ToString());
            }
        }

        private void AVMSCom_MessageSend(object sender, MessageEventArgs e)
        {
            MessageHandler handler = new MessageHandler(Message);
            this.Invoke(handler, new object[] { e });
        }
        public void Message(MessageEventArgs e)
        {
            string message = string.Empty;
            string status = string.Empty;

            message = e.Message;
            if ((string.Empty == message) || (2 != message.Split('\t').Length))
            {
                MessageBox.Show("Not invalid message");
                return;
            }

            m_bConnectedToAVMSServer = m_avms.IsConnected;
            status = string.Format("{0} success to {1} : avms connection is {2}", message.Split('\t')[0], message.Split('\t')[1], m_bConnectedToAVMSServer);
            Trace.WriteLine(status);
            this.toolStripStatusLabel2.Text = status;
            SetConnectButton(!m_bConnectedToAVMSServer, m_bConnectedToAVMSServer);
            UpdateControls(m_bConnectedToAVMSServer);
        }

        private void AddDeviceModelEventHandler(CDeviceManager deviceManager, ref bool bHandleAdded)
        {
            if ((null != deviceManager) && (!bHandleAdded))
            {
                deviceManager.DataLoadedEvent += new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent);
                bHandleAdded = true;
            }
        }
        private void DeleteDeviceModelEventHandler(CDeviceManager deviceManager, ref bool bHandleAdded)
        {
            if ((null != deviceManager) && bHandleAdded)
            {
                deviceManager.DataLoadedEvent -= new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent);
                bHandleAdded = false;
            }
        }

        private void AddAVMSListenerEventHandler(ref bool bHandleAdded)
        {
            if ((null != m_alarmMonitor) 
                && (null != m_eventMonitor) 
                && (!bHandleAdded))
            {
                m_alarmMonitor.AlarmReceived += new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                m_eventMonitor.EventReceived += new EventHandler<EventMessageEventArgs>(HandleEventMessageReceived);
                bHandleAdded = true;
            }
        }
        private void DeleteAVMSListenerEventHandler(ref bool bHandleAdded)
        {
            if ((null != m_alarmMonitor)
                && (null != m_eventMonitor)
                && (bHandleAdded))
            {
                m_alarmMonitor.AlarmReceived -= new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                m_eventMonitor.EventReceived -= new EventHandler<EventMessageEventArgs>(HandleEventMessageReceived);
                bHandleAdded = false;
            }
        }

        private void StartAVMSListener()
        {
            if (null == m_alarmMonitor)
            {
                m_alarmMonitor = new AlarmMonitor(m_farm);
            }
            // Access event could not be supported in AlarmMonitor
            if (null == m_eventMonitor)
            {
                m_eventMonitor = new EventMonitor(m_farm);
            }
            AddAVMSListenerEventHandler(ref m_bAVMSListenerEventHandlerAdded);
        }

        private void StopAVMSListener()
        {
            DeleteAVMSListenerEventHandler(ref m_bAVMSListenerEventHandlerAdded);
            m_alarmMonitor = null;
            m_eventMonitor = null;
        }

        private void btnConnect_Click(object sender, System.EventArgs e)
        {
            //if (null != currentJob)
            //{
            //    this.toolStripStatusLabel3.Text = currentJob.m_jobName + " : AVMS Connect";
            //    SetConnectButton(false, m_bConnectedToAVMSServer);
            //    //currentJob.m_avms.MessageSend += new AVMSCom.MessageEventHandler(this.AVMSCom_MessageSend);
            //    currentJob.m_avms.Connect();

            //    return;
            //}

            //this.toolStripStatusLabel3.Text = "Connect";

            //Release();
            //SetConnectButton(false, m_bConnectedToAVMSServer);
            //m_avms = new AVMSCom(Ip, User, Password);
            //m_avms.MessageSend += new AVMSCom.MessageEventHandler(this.AVMSCom_MessageSend);
            //m_avms.Connect();

            this.toolStripStatusLabel3.Text = rbMultiJob.Checked ? currentJob.m_jobName : "overall" + " : ready to connect AVMS";
            
            if (rbOverall.Checked)
            {
                currentJob = null;
            }
            else if (rbMultiJob.Checked)
            {
                int jobId = GetCurrectJobId();
                if (jobId > 0)
                {
                    currentJob = (ServiceJob)hashJobs[jobId];
                    SetConnectButton(!currentJob.m_bConnectedToAVMSServer, currentJob.m_bConnectedToAVMSServer);
                    UpdateControls(currentJob.m_bConnectedToAVMSServer);
                }
            }
            
            Connect(currentJob);
        }

        private int GetCurrectJobId()
        {
            if (1 != lvJobs.SelectedItems.Count)
            {
                return -1;
            }
            int jobId = -1;
            bool bRet = int.TryParse(lvJobs.SelectedItems[0].Text, out jobId);
            if (bRet)
            {
                return -2;
            }
            return jobId;
        }

        private void Connect(ServiceJob job)
        {
            if (null != job)
            {
                job.InitAVMSServer("127.0.0.1", "admin", "admin");
                if (null != job.m_avms)
                {
                    job.m_avms.Connect();
                }
                
            }
            else
            {
                m_avms = new AVMSCom(Ip, User, Password);
                m_avms.MessageSend += new AVMSCom.MessageEventHandler(this.AVMSCom_MessageSend);
                m_avms.Connect();
            }
        }

        private void Disconnect(ServiceJob job)
        {
            if (null != job)
            {
                if (null != job.m_avms)
                {
                    DeleteDeviceModelEventHandler(job.m_avms.DeviceManager, ref job.m_bDeviceModelEventHandlerAdded);
                    job.m_avms.Disconnect();
                }
            }
            else
            {
                DeleteDeviceModelEventHandler(m_deviceManager, ref m_bDeviceModelEventHandlerAdded);
                m_avms.Disconnect();
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel3.Text = rbMultiJob.Checked ? currentJob.m_jobName : "overall" + " : ready to disconnect AVMS";

            if (rbOverall.Checked)
            {
                currentJob = null;
            }
            else if (rbMultiJob.Checked)
            {
                int jobId = GetCurrectJobId();
                if (jobId > 0)
                {
                    currentJob = (ServiceJob)hashJobs[jobId];
                }
            }

            StopAVMSListener();
            Disconnect(currentJob);

            m_camera = null;
            m_bListened = false;
            btnListen.Text = (m_bListened ? "Listening..." : "Start Listener");

            Release();


            //if (null != currentJob)
            //{
            //    this.toolStripStatusLabel3.Text = currentJob.m_jobName + " : AVMS Disconnect";

            //    if (null == currentJob.m_avms)
            //    {
            //        MessageBox.Show("Fail to connect to AVMS!");
            //        return;
            //    }

            //    DeleteDeviceModelEventHandler(m_deviceManager, ref m_bDeviceModelEventHandlerAdded);
            //    StopAVMSListener();
            //    currentJob.m_avms.Disconnect();

            //    m_camera = null;
            //    m_bConnectedToAVMSServer = false;
            //    m_bListened = false;
            //    btnListen.Text = (m_bListened ? "Listening..." : "Start Listener");

            //    return;
            //}


            //this.toolStripStatusLabel3.Text = "Disconnect";

            //if (null == m_avms)
            //{
            //    MessageBox.Show("Fail to connect to AVMS!");
            //    return;
            //}

            //DeleteDeviceModelEventHandler(m_deviceManager, ref m_bDeviceModelEventHandlerAdded);
            //StopAVMSListener();
            //m_avms.Disconnect();

            //m_camera = null;
            //m_bConnectedToAVMSServer = false;
            //m_bListened = false;
            //btnListen.Text = (m_bListened ? "Listening..." : "Start Listener");

            //Release();
        }

        private void Server_StateChanged(object sender, Seer.BaseLibCS.ValueChangedEventArgs<CServer.ServerState> e)
        {
            if (e.NewValue == e.PreviousValue)
                return;

            if (e.NewValue == CServer.ServerState.Connected)
                m_waitForServerConnection.Set();
        }

        private void DeviceManager_DataLoadedEvent(object sender, EventArgs e)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent), sender, e);
                    return;
                }

                List<CCamera> cameras = m_deviceManager.GetAllCameras();
                PopulateCameraList(cameras);
                SelectDefaultDevice();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load devices : " + ex.ToString());
            }
        }

        private void cbServers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_bServerListPopulating || (m_camera == cbServers.SelectedItem))  // m_server
            {
                return;
            }
            m_iServerId = cbServers.SelectedIndex;
        }

        private void bnJpg_Click(object sender, EventArgs e)
        {
            Snapshot();
        }

        private void bnStartRecording_Click(object sender, EventArgs e)
        {
            if (m_camera == null)
                return;
            m_camera.StartRecording();
        }

        private void bnStopRecording_Click(object sender, EventArgs e)
        {
            if (m_camera == null)
                return;
            m_camera.StopRecording();
        }

        private void bnResumeRecordMode_Click(object sender, EventArgs e)
        {
            if (m_camera == null)
                return;
            m_camera.ResumeRecording();
        }

        private void btnMarkAlarm_Click(object sender, EventArgs e)
        {
            MarkAlarm();
        }

        private void btnRefreshAlarms_Click(object sender, EventArgs e)
        {
            m_bNeedFocus = false;   // flag
            RefreshAlarms();
        }


        private void HandleEventMessageReceived(object sender, EventMessageEventArgs e)
        {
            CameraMessageStruct cameraMessageStruct = e.Message;

            CameraLogStruct cameraLogStruct = new CameraLogStruct();
            cameraLogStruct.m_iAlarmDbId = cameraMessageStruct.m_iAlarmDbId;
            cameraLogStruct.m_iCameraId = cameraMessageStruct.m_iCameraId;
            cameraLogStruct.m_iEvent = cameraMessageStruct.m_iEvent;
            cameraLogStruct.m_iFarmId = cameraMessageStruct.m_iFarmId;
            cameraLogStruct.m_iFGCount = cameraMessageStruct.m_iFGCount;
            cameraLogStruct.m_iNotUsed2 = cameraMessageStruct.m_iNotUsed2;
            cameraLogStruct.m_iPolicyId = cameraMessageStruct.m_iPolicyId;
            cameraLogStruct.m_iState = cameraMessageStruct.m_iState;
            cameraLogStruct.m_iVersion = cameraMessageStruct.m_iVersion;
            cameraLogStruct.m_milliSinceChangeBegan = cameraMessageStruct.m_milliSinceChangeBegan;
            cameraLogStruct.m_milliTime = cameraMessageStruct.m_milliTime;
            cameraLogStruct.m_timezoneTime = cameraMessageStruct.m_timezoneTime;
            cameraLogStruct.m_utcTime = cameraMessageStruct.m_utcTime;
            cameraLogStruct.m_strConfirm = "Not Confirmed";
            cameraLogStruct.m_strComment = string.Empty;

            m_bNeedFocus = true;   // flag
            string log = MakeAlarmLog(cameraLogStruct);
            AddAlarmLog(log);
            m_bNeedFocus = false;   // flag
        }

        private void HandleAlarmMessageReceived(object sender, AlarmMessageEventArgs e)
        {
            CameraMessageStruct cameraMessageStruct = e.Message;

            CameraLogStruct cameraLogStruct = new CameraLogStruct();
            cameraLogStruct.m_iAlarmDbId = cameraMessageStruct.m_iAlarmDbId;
            cameraLogStruct.m_iCameraId = cameraMessageStruct.m_iCameraId;
            cameraLogStruct.m_iEvent = cameraMessageStruct.m_iEvent;
            cameraLogStruct.m_iFarmId = cameraMessageStruct.m_iFarmId;
            cameraLogStruct.m_iFGCount = cameraMessageStruct.m_iFGCount;
            cameraLogStruct.m_iNotUsed2 = cameraMessageStruct.m_iNotUsed2;
            cameraLogStruct.m_iPolicyId = cameraMessageStruct.m_iPolicyId;
            cameraLogStruct.m_iState = cameraMessageStruct.m_iState;
            cameraLogStruct.m_iVersion = cameraMessageStruct.m_iVersion;
            cameraLogStruct.m_milliSinceChangeBegan = cameraMessageStruct.m_milliSinceChangeBegan;
            cameraLogStruct.m_milliTime = cameraMessageStruct.m_milliTime;
            cameraLogStruct.m_timezoneTime = cameraMessageStruct.m_timezoneTime;
            cameraLogStruct.m_utcTime = cameraMessageStruct.m_utcTime;
            cameraLogStruct.m_strConfirm = "Not Confirmed";
            cameraLogStruct.m_strComment = string.Empty;

            m_bNeedFocus = true;   // flag
            string log = MakeAlarmLog(cameraLogStruct);
            AddAlarmLog(log);
            m_bNeedFocus = false;   // flag

            // focus on the last record
            //listAlarms.SelectedIndex = listAlarms.Items.Count - 1;
            // need invoke in different thread
        }


        private void btnInsertAlarm_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(InsertAlarm());
            Trace.WriteLine(InsertAlarm());
        }

        private void cbCurrentTime_CheckedChanged(object sender, EventArgs e)
        {
            tbUTCTime.Enabled = !cbCurrentTime.Checked;
        }

        private void cbPopup_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cbRecording_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void btnGetServers_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel3.Text = "Get Servers";

            if (null == m_avms)
            {
                MessageBox.Show("Fail to connect to AVMS!");
                return;
            }

            try
            {
                //string[] servers = AttemptConnection();
                string[] servers = m_avms.ServerList;
                PopulateServerList(servers);
                SelectDefaultServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to farm: " + ex.Message);
            }
        }

        private void btnGetDevices_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel3.Text = "Get Devices";

            string sStatus = string.Empty;
            if (string.Empty != (sStatus = RefreshDeviceManager()))
            {
                MessageBox.Show(sStatus);
                return;
            }
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            if (!m_bListened)
            {
                StartAVMSListener();
            }
            else
            {
                StopAVMSListener();
            }

            m_bListened = !m_bListened;
            btnListen.Text = (m_bListened ? "Listening..." : "Start Listener");
        }

        private void lvDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (1 != lvDevices.SelectedItems.Count)
            {
                return;
            }

            m_iCameraId = lvDevices.SelectedItems[0].Index;
            uint camId;
            bool bRet = uint.TryParse(lvDevices.SelectedItems[0].Text, out camId);
            if (bRet)
            {
                m_camera = m_deviceManager.GetCameraById(camId);
                // pass to InsertAlarm module
                tbCameraID.Text = camId.ToString();
            }
        }

        private void btnClearAlarms_Click(object sender, EventArgs e)
        {
            listAlarms.Items.Clear();
        }

        private void tbCameraID_KeyDown(object sender, KeyEventArgs e)
        {
            // for linkage with device list selection
            if (1 == lvDevices.SelectedItems.Count)
            {
                lvDevices.SelectedItems[0].Selected = false;
            }
        }

        private void listAlarms_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (null == listAlarms.SelectedItem)
            {
                return;
            }

            tbAlarmMessage.Text = listAlarms.SelectedItem.ToString();

            // just for test
            if (m_bAutoTest)
            {
                btnParseAlarm_Click(sender, e);
            }
        }

        #endregion


        private string Query(string table_name, string field, string condition, out ArrayList record_list)
        {
            return Query(table_name, new string[] { field }, new string[] { condition }, out record_list);
        }

        private string Query_remote(string table_name, string field, string condition, out ArrayList record_list)
        {
            return Query_remote(table_name, new string[] { field }, new string[] { condition }, out record_list);
        }

        private string Query(string table_name, string[] fields, string[] conditions, out ArrayList record_list)
        {
            try
            {
                string statement = string.Empty;
                int count_fields = fields.Length;
                if (0 == count_fields)
                {
                    record_list = null;
                    return "Invalid Statement";
                }
                statement += "SELECT ";
                for (int i = 0; i < count_fields; i++)
                {
                    if (0 != i)
                    {
                        statement += ",";
                    }
                    statement += fields[i];
                }
                statement += " FROM " + table_name;

                int count_conditions = conditions.Length;
                for (int i = 0; i < count_conditions; i++)
                {
                    if (0 == i)
                    {
                        statement += " WHERE ";
                    }
                    else
                    {
                        statement += " AND ";
                    }
                    statement += conditions[i];
                }

                using (var myConn = VmsDatabase.CreateConnection())
                using (var cmd = myConn.CreateCommand(statement))
                {
                    record_list = new ArrayList();

                    cmd.CommandTimeout = 10000;
                    myConn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string[] record = new string[count_fields];
                            string val = string.Empty;
                            for (int i = 0; i < count_fields; i++)
                            {
                                val = reader[fields[i]].ToString();
                                record[i] = val;
                            }
                            record_list.Add(record);
                        }
                        reader.Close();
                    }
                    myConn.Close();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                record_list = null;
                return "Failed to connect to avms database : " + ex.Message;
            }
        }

        private string Query_remote(string table_name, string[] fields, string[] conditions, out ArrayList record_list)
        {
            try
            {
                string statement = string.Empty;
                int count_fields = fields.Length;
                if (0 == count_fields)
                {
                    record_list = null;
                    return "Invalid Statement";
                }
                statement += "SELECT ";
                for (int i = 0; i < count_fields; i++)
                {
                    if (0 != i)
                    {
                        statement += ",";
                    }
                    statement += fields[i];
                }
                statement += " FROM " + table_name;

                int count_conditions = conditions.Length;
                for (int i = 0; i < count_conditions; i++)
                {
                    if (0 == i)
                    {
                        statement += " WHERE ";
                    }
                    else
                    {
                        statement += " AND ";
                    }
                    statement += conditions[i];
                }

                //using (var myConn = VmsDatabase.CreateConnection())
                // remote connect via connectionString (need provide Password)
                string connectionString = "Data Source=.\\AXIS;Initial Catalog=VMSChina1;User ID=sa;Password=Axis123456";
                VmsDatabase db = new VmsDatabase(connectionString);
                using (var myConn = db.OpenConnection())
                using (var cmd = myConn.CreateCommand(statement))
                {
                    record_list = new ArrayList();

                    cmd.CommandTimeout = 10000;
                    //myConn.Open();    // no need open again for the state has already been open
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string[] record = new string[count_fields];
                            string val = string.Empty;
                            for (int i = 0; i < count_fields; i++)
                            {
                                val = reader[fields[i]].ToString();
                                record[i] = val;
                            }
                            record_list.Add(record);
                        }
                        reader.Close();
                    }
                    myConn.Close();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                record_list = null;
                return "Failed to connect to avms database : " + ex.Message;
            }
        }

        private void btnParseAlarm_Click(object sender, EventArgs e)
        {
            string msg = tbAlarmMessage.Text;

            string[] elements = msg.Split('|');
            string policy_type_id = string.Empty;
            string alarm_type_id = string.Empty;
            // policy type id
            if ((elements.Length > 2) && IsInteger(policy_type_id = elements[2].Trim()))
            {
                tbPolicyType.Text = policy_type_id;
            }
            else
            {
                tbPolicyType.Text = string.Empty;
            }
            // alarm type id
            if ((elements.Length > 3) && IsInteger(alarm_type_id = elements[3].Trim()))
            {
                tbAlarmType.Text = alarm_type_id; 
            }
            else
            {
                tbAlarmType.Text = string.Empty;
            }

            // just for test
            if (m_bAutoTest)
            {
                btnProcessAlarm_Click(sender, e);
            }
        }

        private void btnProcessAlarm_Click(object sender, EventArgs e)
        {
            string policy_type_id = tbPolicyType.Text;
            string alarm_type_id = tbAlarmType.Text;
            // policy type
            if ((string.Empty != policy_type_id) && IsInteger(policy_type_id))
            {
                tbPolicyType.Text = ToPolicyType(int.Parse(policy_type_id));
            }
            else
            {
                tbPolicyType.Text = string.Empty;
            }
            // alarm type
            if ((string.Empty != alarm_type_id) && IsInteger(alarm_type_id))
            {
                tbAlarmType.Text = ToAlarmType(int.Parse(alarm_type_id));
            }
            else
            {
                tbAlarmType.Text = string.Empty;
            }

            // just for test
            if (m_bAutoTest)
            {
                btnFilter_Click(sender, e);
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            if (m_bDatabaseAccessAllowed)
            {
                if (string.Empty == m_strPolicyDesc)
                {
                    //MessageBox.Show("Nothing needs to be filtered!");
                    Trace.WriteLine("Nothing needs to be filtered!");
                    return;
                }

                /* xmlText of policy */
                //<policy>
                //	<action>22</action>
                //	<schedule>8</schedule>
                //	<priority>5</priority>
                //	<events>
                //		<type>SEQ</type>
                //		<period>30</period>
                //		<loc seq="0">23</loc>
                //		<loc seq="1">20</loc>
                //  </events>
                //</policy>
                //string[] policy_values = GetValues(m_strPolicyDesc, "action", "loc");
                //string action_id = policy_values[0];
                //string event_loc_id = policy_values[1];
                // fail to get value with attribution, so change to use XmlDocument
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(m_strPolicyDesc);
                XmlNode actionsNode = doc.SelectSingleNode("policy/action");
                if (null == actionsNode)
                {
                    return;
                }
                string action_id = actionsNode.InnerText;
                List<string> event_loc_id_list = new List<string>(); // not sure whether seq no is in order, so not use string[]
                XmlNode eventsNode = doc.SelectSingleNode("policy/events");
                if (null == eventsNode)
                {
                    return;
                }
                XmlNodeList itemNodeList = eventsNode.ChildNodes;
                if (null != itemNodeList)
                {
                    foreach (XmlNode itemNode in itemNodeList)
                    {
                        if ("loc" == itemNode.Name)
                        {
                            event_loc_id_list.Add(itemNode.InnerText);
                        }
                    }
                }
                // get action name of policy
                ArrayList list;
                Query("Policy", "Nm", "Id=" + int.Parse(action_id), out list);
                if (1 == list.Count)
                {
                    m_strRuleAction = ((string[])list[0])[0];
                    m_enumAlarmType = (AlarmType)Enum.Parse(typeof(AlarmType), m_strRuleAction, true);
                }
                else
                {
                    m_strRuleAction = string.Empty;
                }
                tbRuleAction.Text = m_strRuleAction;
                // get event id by matching event loc id
                m_listEventIds.Clear();
                string event_id = string.Empty;
                foreach (string loc_id in event_loc_id_list)
                {
                    switch (loc_id)
                    {
                        case "20":
                            event_id = "4";
                            break;
                        case "23":
                            event_id = "4";
                            break;
                        default:
                            break;
                    }

                    if (!m_listEventIds.Contains(event_id))
                    {
                        m_listEventIds.Add(event_id);
                    }
                }

                // filter action events
                if (0 == m_listEventIds.Count)
                {
                    MessageBox.Show("No event needs to be taken action!");
                    return;
                }
                m_mapActionEvents.Clear();
                EventCollection event_list = DeserializeFromXml<EventCollection>(RULE_EVENT_CONFIG);
                foreach (Event et in event_list.EventList)
                {
                    event_id = et.event_id;
                    if (m_listEventIds.Contains(event_id))
                    {
                        ArrayList action_list = et.actions;
                        m_mapActionEvents.Add(int.Parse(event_id), action_list);
                    }
                }
                // show in rule action list
                listRuleAction.Items.Clear();
                foreach (int id in m_mapActionEvents.Keys)
                {
                    ArrayList actionList = m_mapActionEvents[id];
                    for (int i = 0; i < actionList.Count; i++)
                    {
                        EventAction action = actionList[i] as EventAction;
                        //string item = String.Format("action type[{0}]\tcommand[{1}]", action.action_type, action.command); // temp modify
                        string item = string.Empty;
                        AddEventActions(item);
                    }
                }
            }
            else
            {
                // filter action events
                if (0 == m_listEventIds.Count)
                {
                    MessageBox.Show("No event needs to be taken action!");
                    return;
                }

                m_mapActionEvents.Clear();
                // according to policy id
                m_strRuleAction = string.Empty;
                EventCollection event_list = DeserializeFromXml<EventCollection>(RULE_EVENT_CONFIG);
                if ((null == event_list.RuleList) || (0 == event_list.RuleList.Count()))
                {
                    return;
                }
                foreach (Event et in event_list.EventList)
                {
                    string event_id = et.event_id;
                    if (m_listEventIds.Contains(event_id))
                    {
                        m_strRuleAction += '[' + et.event_name + ']';
                        ArrayList action_list = et.actions;
                        m_mapActionEvents.Add(int.Parse(event_id), action_list);
                    }
                }
                tbRuleAction.Text = m_strRuleAction;
                // show in rule action list
                listRuleAction.Items.Clear();
                foreach (int id in m_mapActionEvents.Keys)
                {
                    ArrayList actionList = m_mapActionEvents[id];
                    for (int i = 0; i < actionList.Count; i++)
                    {
                        EventAction action = actionList[i] as EventAction;
                        List<Command> commands = action.commands;
                        foreach (Command command in commands)
                        {
                            string item = String.Empty;
                            if (null != command.command_body)
                            {
                                item = String.Format("Action {0}[{1}]_Step {2}[{3}]\t{4}\t{5}\t{6}",
                                        action.action_id, action.action_type, command.command_id, command.command_desc, command.command_method, command.command_url, command.command_body);
                            }
                            else
                            {
                                item = String.Format("Action {0}[{1}]_Step {2}[{3}]\t{4}\t{5}",
                                        action.action_id, action.action_type, command.command_id, command.command_desc, command.command_method, command.command_url);
                            }
                            AddEventActions(item);
                        }
                    }
                }
            }

            // just for test
            if (m_bAutoTest)
            {
                for (int i = 0; i < listRuleAction.Items.Count; i++)
                {
                    listRuleAction.SelectedIndex = i;
                    btnSend_Click(null, new EventArgs());
                }
            }
        }

        public void FocusLastRecord<T>(T t)
        {
            Type type = t.GetType();
            if ("ListBox" == type.Name)
            {
                ListBox control = t as ListBox;
                if (0 < control.Items.Count)
                {
                    control.SelectedIndex = control.Items.Count - 1;
                }
            }
        }
        public void FocusRecord<T>(T t, int index)
        {
            Type type = t.GetType();
            if ("ListBox" == type.Name)
            {
                ListBox control = t as ListBox;
                if (0 < control.Items.Count)
                {
                    control.SelectedIndex = index;
                }
            }
        }

        private void listRuleAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            string item = listRuleAction.SelectedItem.ToString();
            string command = string.Empty;
            if ((2 == item.Split('\t').Length) && ((command = item.Split('\t')[1]).Contains("command")))
            {
                string action = item.Split('\t')[0];
                string action_type = action.Substring(action.IndexOf('[') + 1, action.IndexOf(']') - action.IndexOf('[') - 1);
                if ("play_audio" == action_type)
                {
                    SelectExecuteAction(1); // 0 (Get/Post are both CURLE_OK)
                    tbBody.Text = string.Empty;
                }
                else if ("open_door" == action_type)
                {
                    SelectExecuteAction(1);
                    tbBody.Text = "{\"tdc:AccessDoor\": {\"Token\": \"Axis-accc8e2477fb:1533314560.049295000\"}}";
                }
                else
                {
                    SelectExecuteAction(0);
                    tbBody.Text = string.Empty;
                }
                string url = command.Substring(command.IndexOf('[')+1, command.IndexOf(']')-command.IndexOf('[')-1);
                tbURL.Text = url;
            }
        }

        private void AddEventActions(string log)
        {
            if (InvokeRequired)
            {
                Invoke(new System.Action<string>(AddEventActions), log);
                return;
            }

            listRuleAction.Items.Add(log);
            //// focus on the latest record
            //listRuleAction.SelectedIndex = listRuleAction.Items.Count - 1;
        }

        //private static bool IsNumber(string s)
        private static bool IsInteger(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }
            //const string pattern = "^[/.0-9]*$";
            const string pattern = "^[0-9]*$";
            Regex rx = new Regex(pattern);
            return rx.IsMatch(s);
        }

        private string ToPolicyType(int typeId)
        {
            string type = string.Empty;

            if (m_bDatabaseAccessAllowed)
            {
                ArrayList list;
                Query("Policy", new string[] { "Nm", "XML" }, new string[] { "Id=" + typeId, "Typ='policy'" }, out list);
                if (1 == list.Count)
                {
                    type = ((string[])list[0])[0];
                    m_strPolicyDesc = ((string[])list[0])[1];
                }
                else
                {
                    type = "#NO VALUE";
                    m_strPolicyDesc = string.Empty;
                }
            }
            else
            {
                EventCollection config_list = DeserializeFromXml<EventCollection>(RULE_EVENT_CONFIG);
                if ((null == config_list) || (null == config_list.RuleList) || (0 == config_list.RuleList.Count()))
                {
                    type = "#NO VALUE";
                }
                else
                {
                    // get event id via config file
                    m_listEventIds.Clear();
                    foreach (RulePolicy rule in config_list.RuleList)
                    {
                        type = "#NO VALUE";
                        if (typeId.ToString() == rule.rule_id)
                        {
                            type = rule.rule_name;
                            string[] event_ids = rule.event_id.Split(',');
                            foreach (string event_id in event_ids)
                            {
                                if (!m_listEventIds.Contains(event_id))
                                {
                                    m_listEventIds.Add(event_id);
                                }
                            }

                            break;
                        }
                    }
                }
            }

            return type;
        }

        private string ToAlarmType(int typeId)
        {
            string type = string.Empty;

            if (m_bDatabaseAccessAllowed)
            {
                ArrayList list;
                Query("AlarmTypes", "Nm", "Id=" + typeId, out list);
                if (1 == list.Count)
                {
                    type = ((string[])list[0])[0];
                    m_enumAlarmType = (AlarmType)Enum.Parse(typeof(AlarmType), type, true);
                }
                else
                {
                    m_enumAlarmType = AlarmType.UNKNOWN;
                    // get description of m_enumAlarmType
                    Type enumType = m_enumAlarmType.GetType();
                    //FieldInfo fieldInfo = enumType.GetField(Enum.GetName(typeof(AlarmType), 1));  // need judge whether the type exists in Enum
                    string name = Enum.GetName(typeof(AlarmType), 1);
                    if (null == name)
                    {
                        return string.Empty;
                    }
                    FieldInfo fieldInfo = enumType.GetField(name);
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                    type = attr.Description;
                }
            }
            else
            {
                type = Enum.GetName(typeof(AlarmType), typeId);
                if (null == type)
                {
                    m_enumAlarmType = AlarmType.UNKNOWN;
                    type = Enum.GetName(typeof(AlarmType), m_enumAlarmType);
                }
                else
                {
                    m_enumAlarmType = (AlarmType)Enum.Parse(typeof(AlarmType), type, true);
                }
            }

            return type;
        }

        private static string[] GetValues(string xmlText, params string[] keys)
        {
            int count = keys.Length;
            if (0 == count)
            {
                return null;
            }
            
            string[] values = new string[count];
            for (int i=0; i<count; i++)
            {
                // functions of Utils.cs (null if key does not exist)
                values[i] = Utils.GetXmlValue(xmlText, keys[i]);
            }
            return values;
        }

        // XMLDocument
        private static void ReadXML1()
        {
            string xmlFilePath = @"Events.conf";
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);

            // xpath
            XmlNodeList eventNodeList = doc.SelectNodes("configuration/Events/event");
            if (null != eventNodeList)
            {
                foreach (XmlNode eventNode in eventNodeList)
                {
                    // event id
                    //string event_id = eventNode.Attributes["id"].Value; // need judge whether id exists
                    XmlAttributeCollection node = eventNode.Attributes;
                    bool isExist = false;
                    string event_id = string.Empty;
                    for (int i = 0; i < node.Count; i++)
                    {
                        if ("id" == node[i].Name)
                        {
                            isExist = true;
                            event_id = node[i].Value;
                            break;
                        }
                    }
                    if (!isExist)
                    {
                        continue;
                    }

                    // event action list
                    XmlNode actionsNode = eventNode.SelectSingleNode("actions");
                    if (null == actionsNode)
                    {
                        continue;
                    }
                    XmlNodeList actionNodeList = actionsNode.ChildNodes;
                    if (null != actionNodeList)
                    {
                        foreach (XmlNode actionNode in actionNodeList)
                        {
                            string action_id = actionNode.Attributes["id"].Value;
                            string action_type = actionNode.Attributes["type"].Value;
                            // action command list
                            XmlNodeList commandNodeList = actionNode.ChildNodes;
                            if (null == commandNodeList)
                            {
                                continue;
                            }
                            foreach (XmlNode commandNode in commandNodeList)
                            {
                                string command_id = commandNode.Attributes["id"].Value;
                                string command_desc = commandNode.Attributes["desc"].Value;
                                XmlNode methodNode = commandNode.FirstChild;
                                //XmlCDataSection cdata = (XmlCDataSection)methodNode.FirstChild;
                                //string command_method = cdata.InnerText;
                                Type element_type;
                                element_type = methodNode.FirstChild.GetType();   // Name:XmlText, FullName:System.Xml.XmlText; XmlElement;
                                if ("XmlText" != element_type.Name)
                                {
                                    continue;
                                }
                                string command_method = methodNode.FirstChild.Value; // the same as methodNode.FirstChild.InnerText
                                // methodNode.FirstChild.Name (#text), methodNode.FirstChild.ToString() (System.Xml.XmlText)
                                XmlNode urlNode = null;
                                XmlNode bodyNode = null, bodyElementsNode = null;
                                string command_url = string.Empty;
                                string command_body = string.Empty;
                                Dictionary<string, string> command_body_elements = new Dictionary<string, string>();
                                // two kind of command_method
                                switch (command_method)
                                {
                                    case "GET":

                                        urlNode = commandNode.SelectSingleNode("url");
                                        if ((null != urlNode) && ("XmlText" == urlNode.FirstChild.GetType().Name))
                                        {
                                            command_url = urlNode.FirstChild.Value;
                                        }

                                        break;

                                    case "POST":

                                        urlNode = commandNode.SelectSingleNode("url");
                                        if ((null != urlNode) && ("XmlText" == urlNode.FirstChild.GetType().Name))
                                        {
                                            command_url = urlNode.FirstChild.Value;
                                        }
                                        bodyNode = commandNode.SelectSingleNode("body");
                                        if ((null != bodyNode) && ("XmlText" == bodyNode.FirstChild.GetType().Name))
                                        {
                                            command_body = bodyNode.FirstChild.Value;
                                        }
                                        bodyElementsNode = commandNode.SelectSingleNode("body_elements");
                                        if ((null != bodyElementsNode) && ("XmlElement" == bodyNode.FirstChild.GetType().Name))
                                        {
                                            XmlNodeList elementNodeList = bodyElementsNode.ChildNodes;
                                            string execute_type = string.Empty;
                                            string door_id = string.Empty;
                                            foreach (XmlNode elementNode in elementNodeList)
                                            {
                                                if (("execute_type" == elementNode.Name) && ("XmlText" == elementNode.FirstChild.GetType().Name))
                                                {
                                                    execute_type = elementNode.FirstChild.Value;
                                                }
                                                else if (("door_id" == elementNode.Name) && ("XmlText" == elementNode.FirstChild.GetType().Name))
                                                {
                                                    door_id = elementNode.FirstChild.Value;
                                                }
                                            }
                                            if ((string.Empty != execute_type) && (string.Empty != door_id))
                                            {
                                                command_body_elements.Add(execute_type, door_id);
                                            }
                                        }

                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // XML Reader
        private static void ReadXML2()
        {
            //using (StringReader strRdr = new StringReader(@"Events.conf"))  // replace detailed xml content with actual file, but NG
            using (StreamReader strRdr = new StreamReader(@"Events.conf"))
            {
                using (XmlReader rdr = XmlReader.Create(strRdr))
                {
                    while (rdr.Read())
                    {
                        // node type
                        XmlNodeType nodeType = rdr.NodeType;
                        if (XmlNodeType.Element == nodeType)    // <XXX>
                        {
                            string elementName = rdr.Name;  // element start
                            if ("configuration" == elementName)
                            {
                                //
                            }
                            else if ("Events" == elementName)
                            {
                                //
                            }
                            else if ("event" == elementName)
                            {
                                string event_id = rdr["id"];
                            }
                            else if ("actions" == elementName)
                            {
                                //
                            }
                            else if ("action" == elementName)
                            {
                                string action_id = rdr["id"];
                                string action_type = rdr["type"];
                            }
                            else if ("command" == elementName)
                            {
                                string command = string.Empty;
                                // get content of element
                                if (rdr.Read())
                                {
                                    command = rdr.Value;
                                }
                            }
                        }
                        else if (XmlNodeType.EndElement == nodeType) // </XXX>
                        {
                            string elementName = rdr.Name;  // element end
                        }
                    }
                }
            }
        }

        // XMLSerializer
        private void ReadXML3()
        {
            EventCollection event_list = DeserializeFromXml<EventCollection>("transaction.conf");
            foreach (RulePolicy rule in event_list.RuleList)
            {
                string rule_id = rule.rule_id;
                string rule_name = rule.rule_name;
                string event_id = rule.event_id;
            }
            foreach (Event et in event_list.EventList)
            {
                string event_id = et.event_id;
                ArrayList action_list = et.actions;
                foreach (EventAction action in action_list)
                {
                    string action_id = action.action_id;    // null
                    string action_type = action.action_type;
                    //string command = action.command;
                    // append detail for command
                    List<Command> commands = action.commands;
                }
                m_mapActionEvents.Add(int.Parse(event_id), action_list);
            }
        }

        //private static void WriteXML3(string filePath)
        //{
        //    EventCollection xml = new EventCollection();
        //    int event_num = 2;
        //    Event[] events = new Event[event_num];
        //    for (int i=0; i< event_num; i++)
        //    {
        //        Event et = new Event();
        //        et.event_id = i.ToString();
        //        int action_num = 0;
        //        string[] action_type_list = new string[] { "open_door", "play_audio", "query_params"};
        //        if (0==i)
        //        {
        //            action_num = 3;
        //        }
        //        else
        //        {
        //            action_num = 1;
        //        }
        //        ArrayList actions = new ArrayList(action_num);
        //        for (int j=0; j<action_num; j++)
        //        {
        //            Action action = new Action();
        //            action.action_id = j.ToString();
        //            action.action_type = action_type_list[j];
        //            switch (action.action_type)
        //            {
        //                case "open_door":
        //                    action.command = "http://192.168.77.156/vapix/doorcontrol";
        //                    break;
        //                case "play_audio":
        //                    action.command = "http://192.168.77.155/axis-cgi/playclip.cgi?location=alarm.mp3";
        //                    break;
        //                case "query_params":
        //                    action.command = "http://192.168.77.243/axis-cgi/param.cgi?action=list";
        //                    break;
        //                default:
        //                    break;
        //            }
        //            actions.Add(action);
        //        }
        //        et.actions = actions;
        //        events[i] = et;
        //    }
        //    xml.EventList = events;

        //    SerializeToXml<EventCollection>(filePath, xml);
        //}
        // append detail for command
        private void WriteXML3(string filePath)
        {
            EventCollection xml = new EventCollection();
            // rules
            ArrayList[] ruleList = new ArrayList[8];
            ruleList[0] = new ArrayList { "3", "摄像机连接丢失", "0" };
            ruleList[1] = new ArrayList { "6", "摄像机篡改", "0" };
            ruleList[2] = new ArrayList { "9", "手动报警", "0" };
            ruleList[3] = new ArrayList { "13", "Lost Camera Connection", "0" };
            ruleList[4] = new ArrayList { "16", "Camera Tamper", "0" };
            ruleList[5] = new ArrayList { "19", "Manual Alarm", "0" };
            ruleList[6] = new ArrayList { "21", "FDFR_cam1", "4" };
            ruleList[7] = new ArrayList { "24", "FDFR_cam2", "4" };
            int rule_num = ruleList.Length;
            RulePolicy[] rules = new RulePolicy[rule_num];
            for (int i = 0; i < rule_num; i++)
            {
                RulePolicy rule = new RulePolicy();
                rule.rule_id = (string)ruleList[i][0];
                rule.rule_name = (string)ruleList[i][1];
                rule.event_id = (string)ruleList[i][2];
                rules[i] = rule;
            }
            xml.RuleList = rules;
            // events
            int event_num = 5;
            Event[] events = new Event[event_num];
            events[0] = new Event();
            events[0].event_id = "0";
            events[0].event_name = "未定义";
            events[1] = new Event();
            events[1].event_id = "1";
            events[1].event_name = "获取设备参数列表";
            events[2] = new Event();
            events[2].event_id = "2";
            events[2].event_name = "未定义";
            events[3] = new Event();
            events[3].event_id = "3";
            events[3].event_name = "未定义";
            events[4] = new Event();
            events[4].event_id = "4";
            events[4].event_name = "人脸检测联动";
            // command list
            Dictionary<string, List<Command>> actionCommands = new Dictionary<string, List<Command>>();
            string action_type = string.Empty;
            //
            action_type = "query_params";
            List<Command> commands_1 = new List<Command>();
            Command command1 = new Command();
            command1.command_id = "1";
            command1.command_desc = "Get param list";
            command1.command_method = "GET";
            command1.command_url = "http://192.168.77.243/axis-cgi/param.cgi?action=list";
            command1.command_body = null;
            command1.body_elements = null;
            commands_1.Add(command1);
            actionCommands.Add(action_type, commands_1);
            //
            action_type = "play_audio";
            List<Command> commands_2 = new List<Command>();
            Command command2 = new Command();
            command2.command_id = "1";
            command2.command_desc = "Execute play clip";
            command2.command_method = "POST";
            command2.command_url = "http://192.168.77.155/axis-cgi/playclip.cgi?location=alarm.mp3";
            command2.command_body = null;
            command2.body_elements = null;
            commands_2.Add(command2);
            actionCommands.Add(action_type, commands_2);
            //
            action_type = "open_door";
            List<Command> commands_3 = new List<Command>();
            Command command3 = new Command();
            command3.command_id = "1";
            command3.command_desc = "Get door tokens";
            command3.command_method = "POST";
            command3.command_url = "http://192.168.77.156/vapix/doorcontrol";
            command3.command_body = "{\"axtdc: GetDoorConfigurationList\":{}}";
            command3.body_elements = null;
            commands_3.Add(command3);
            Command command4 = new Command();
            command4.command_id = "2";
            command4.command_desc = "Execute door control";
            command4.command_method = "POST";
            command4.command_url = "http://192.168.77.156/vapix/doorcontrol";
            command4.command_body = "{\"tdc:AccessDoor\":{\"Token\":\"Axis-accc8e2477fb:1533314560.049295000\"}}";
            command4.body_elements = null;
            commands_3.Add(command4);
            actionCommands.Add(action_type, commands_3);
            // event-action-command
            for (int i = 0; i < event_num; i++)
            {
                ArrayList actions = new ArrayList();
                EventAction action1, action2;
                switch (i)
                {
                    case 0:
                        actions = null;
                        break;
                    case 1:

                        actions.Clear();
                        // 1 action
                        action1 = new EventAction();
                        action1.action_id = "0";
                        action1.action_type = "query_params";
                        action1.commands = actionCommands[action1.action_type];
                        actions.Add(action1);
                        break;

                    case 2:
                    case 3:
                        actions = null;
                        break;
                    case 4:

                        actions.Clear();
                        // 2 actions
                        action1 = new EventAction();
                        action1.action_id = "0";
                        action1.action_type = "open_door";
                        action1.commands = actionCommands[action1.action_type];
                        actions.Add(action1);
                        action2 = new EventAction();
                        action2.action_id = "1";
                        action2.action_type = "play_audio";
                        action2.commands = actionCommands[action2.action_type];
                        actions.Add(action2);
                        break;

                    default:
                        actions = null;
                        break;
                }
                events[i].actions = actions;
            }
            xml.EventList = events;

            SerializeToXml<EventCollection>(filePath, xml);
        }

        public static T DeserializeFromXml<T>(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    throw new ArgumentNullException(filePath + " not Exists");
                }
                using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    T ret = (T)xs.Deserialize(reader);
                    return ret;
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                return default(T);
            }
        }

        public static void SerializeToXml<T>(string filePath, T obj)
        {
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
                {
                    System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                    xs.Serialize(writer, obj);
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                Trace.WriteLine(error);
            }
        }

/*

        //[XmlRoot("Configuration")]
        [XmlType(TypeName = "configuration")]
        public class EventCollection
        {
            [XmlArray("rule_event_map")]
            public Rule[] RuleList { get; set; }

            //[XmlArray("Events"), XmlArrayItem("event")]
            //public Event[] EventList { get; set; }
            [XmlArray("events")]    // the old is "Events"
            public Event[] EventList { get; set; }
        }

        [XmlType(TypeName = "rule")]
        public class Rule
        {
            // Attribute

            [XmlAttribute("id")]
            public string rule_id { get; set; }

            [XmlAttribute("name")]
            public string rule_name { get; set; }

            [XmlText]
            public string event_id { get; set; }    // separated by ','
        }

        //[XmlRoot("event")]  // XmlRootAttribute
        [XmlType(TypeName = "event")]
        public class Event
        {
            // Attribute

            [XmlAttribute("id")]
            public string event_id { get; set; }

            [XmlAttribute("name")]
            public string event_name { get; set; }

            [XmlArray("actions"), XmlArrayItem("action", typeof(Action))]
            public ArrayList actions = new ArrayList();
        }

        //[XmlRoot("action")]
        [XmlType(TypeName = "action")]
        public class Action
        {
            // Attribute

            //[XmlIgnore] // no request to serialize the property of element
            [XmlAttribute("id")]
            public string action_id { get; set; }

            [XmlAttribute("type")]
            //public ActionType action_type { get; set; }   // NG
            public string action_type { get; set; }

            // Sub-Element

            //[XmlElement("command")]
            //public string command { get; set; }
            // =>
            // append detail for command
            // should be XmlElement for there's no commands node, or it'll be XmlArray+XmlArrayItem
            [XmlElement("command", typeof(Command))]
            public List<Command> commands { get; set; }
        }

        // append detail for command
        [XmlType(TypeName = "command")]
        public class Command
        {
            [XmlAttribute("id")]
            public string command_id { get; set; }

            [XmlAttribute("desc")]
            public string command_desc { get; set; }

            [XmlElement("method")]
            public string command_method { get; set; }

            [XmlElement("url")]
            public string command_url { get; set; }

            [XmlElement("body")]
            public string command_body { get; set; }

            [XmlElement("body_elements", typeof(BodyElement))]
            //public ArrayList body_elements = new ArrayList(); // there's only one body_elements node
            public BodyElement body_elements { get; set; }
        }

        [XmlType(TypeName = "body_elements")]
        public class BodyElement
        {
            [XmlElement("execute_type")]
            public string execute_type { get; set; }

            [XmlElement("door_id")]
            public string door_id { get; set; }
        }

*/


        public enum ActionType
        {
            UNKNOWN = 0,
            OPEN_DOOR = 1,
            PLAY_AUDIO = 2
        }

        public enum AlarmType
        {
            [Description("Unknown")]
            UNKNOWN = -1,
            // according to database
            ALARM = 0,
            OBSTRUCTED = 3,
            NONE = 7,
            CORD_CUT = 8,
            //
            CUSTOMIZED = 12,
        }

        public enum ExecuteAction
        {
            GET = 0,
            POST = 1
        }


        private void InitExecuteAction()
        {
            // number -> string
            var name = EnumHelper.GetEnumName<ExecuteAction>(0);
            Trace.WriteLine("Name of the first enum：" + name);
            // enum -> dictionary
            var dic = EnumHelper.getEnumDic<ExecuteAction>();
            foreach (var item in dic)
            {
                Trace.WriteLine(item.Key + "==" + item.Value);
                cbExecuteAction.Items.Add(item.Key);
            }
            if (0 != cbExecuteAction.Items.Count)
            {
                SelectExecuteAction(0);
            }
        }

        private void SelectExecuteAction(int id)
        {
            cbExecuteAction.SelectedIndex = id;
        }

        // check and format json string
        private string ConvertJsonString(string str)
        {
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                Trace.WriteLine("Invalid Json format");
                return string.Empty;
            }
        }


        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                lblResult.Text = string.Empty;
                tbResult.Text = string.Empty;

                string url = tbURL.Text;
                //string post_data = string.Empty;
                string post_data = tbBody.Text;
                post_data = ConvertJsonString(post_data);
                tbBody.Text = post_data;
                if ((string.Empty == url) && (!url.Contains("http")))
                {
                    MessageBox.Show("Not valid command to be executed");
                }

                CgiFactory factory = new CgiFactory();
                factory.CURL_Init();
                factory.CURL_SetUrl(url);
                if ("GET" == cbExecuteAction.SelectedItem.ToString())
                {
                    factory.CURL_SetMethod(CgiFactory.CURL_METHOD.CURL_METHOD_GET);
                }
                else if ("POST" == cbExecuteAction.SelectedItem.ToString())
                {
                    factory.CURL_SetMethod(CgiFactory.CURL_METHOD.CURL_METHOD_POST);
                }
                else
                {
                    return;
                }

                string status = string.Empty;
                string result = string.Empty;
                switch (factory.CURL_GetMethod())
                {
                    case CgiFactory.CURL_METHOD.CURL_METHOD_GET:

                        bool ret = factory.CURL_HTTP_Get(out status, out result);
                        lblResult.Text = status;
                        tbResult.Text = result;
                        break;

                    case CgiFactory.CURL_METHOD.CURL_METHOD_POST:

                        //post_data = "{\"axtdc:GetDoorConfigurationList\":{}}";
                        //post_data = "{\"tdc:AccessDoor\": {\"Token\": \"Axis-accc8e2477fb:1533314560.049295000\"}}";
                        factory.CURL_SetPostData(post_data);
                        ret = factory.CURL_HTTP_Post(out status, out result);
                        lblResult.Text = status;
                        tbResult.Text = result;
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void cbExecuteAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_iExecuteActionValue = (uint)cbExecuteAction.SelectedIndex;
        }



        private static byte[] CRC16_1(Byte[] bytes)
        {
            int count = bytes.Length;

            ushort crc = 0;

            Byte crctemp;

            int i = 0;

            while (count > 0)
            {
                count -= 1;

                crctemp = Convert.ToByte(crc >> 8);

                crc = (ushort)((crc << 8) & 0xFFFF);

                crc = (ushort)((CRC16Table[crctemp ^ bytes[i]] ^ crc) & 0xFFFF);

                i += 1;
            }

            return new byte[] { (byte)(crc >> 8), (byte)(crc & 0xFF) };

        }

        private static byte[] CRC16_2(byte[] data)
        {
            byte[] returnVal = new byte[2];
            byte CRC16Lo, CRC16Hi, CL, CH, SaveHi, SaveLo;
            int i, Flag;
            CRC16Lo = 0xFF;
            CRC16Hi = 0xFF;
            CL = 0x86;
            CH = 0x68;
            for (i = 0; i < data.Length; i++)
            {
                CRC16Lo = (byte)(CRC16Lo ^ data[i]);//每一个数据与CRC寄存器进行异或
                for (Flag = 0; Flag <= 7; Flag++)
                {
                    SaveHi = CRC16Hi;
                    SaveLo = CRC16Lo;
                    CRC16Hi = (byte)(CRC16Hi >> 1);//高位右移一位
                    CRC16Lo = (byte)(CRC16Lo >> 1);//低位右移一位
                    if ((SaveHi & 0x01) == 0x01)//如果高位字节最后一位为
                    {
                        CRC16Lo = (byte)(CRC16Lo | 0x80);//则低位字节右移后前面补 否则自动补0
                    }
                    if ((SaveLo & 0x01) == 0x01)//如果LSB为1，则与多项式码进行异或
                    {
                        CRC16Hi = (byte)(CRC16Hi ^ CH);
                        CRC16Lo = (byte)(CRC16Lo ^ CL);
                    }
                }
            }
            returnVal[0] = CRC16Hi;//CRC高位
            returnVal[1] = CRC16Lo;//CRC低位
            return returnVal;
        }


        private void btnImport_Click(object sender, EventArgs e)
        {
            //Form2 form = new Form2();
            //form.Owner = this;
            //form.init(sender);
            //form.Show();

            //List<CCamera> camList = m_farm.DeviceManager.GetAllCameras();
            //foreach (CCamera cam in camList)
            //{
            //    DateTime dtNow = DateTime.Now;
            //    DateTime dtLastUpdate = cam.LastStateUpdateTime;
            //    TimeSpan ts = dtNow - dtLastUpdate;
            //    double diffSecond = ts.TotalSeconds;
            //    Trace.WriteLine(String.Format("Now : {0}; Last update : {1}; -> diff = {2}", dtNow, dtLastUpdate, diffSecond.ToString()));
            //    if (diffSecond > 65)
            //    {
            //        Trace.WriteLine("Cam" + cam.CameraId + " : Time Out!");
            //    }
            //    bool bTimedOut = cam.TimedOut;
            //    Trace.WriteLine("Cam" + cam.CameraId + " : TimedOut = " + bTimedOut);
            //}


            //Byte[] bytes;
            //Byte[] bytes = { 0x20, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x01, 0x03 };
            //Byte[] bytes = { 0xD0, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02 };
            //Byte[] bytes = { 0xF0, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Byte[] bytes_head = { 0xF0, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x16 };
            Byte[] bytes_data = { 0x30, 0x30, 0x31, 0x2E, 0x33, 0x30, 0x36, 0x2E, 0x30, 0x30, 0x20, 0x64, 0x72, 0x76, 0x5F, 0x31, 0x5F, 0x31, 0x5F, 0x31, 0x5F, 0x31 };
            string data = Encoding.GetEncoding("GB2312").GetString(bytes_data);
            string data_utf = Encoding.GetEncoding("UTF-8").GetString(bytes_data);
            Byte[] bytes = new Byte[bytes_head.Length + bytes_data.Length];
            Array.Copy(bytes_head, bytes, bytes_head.Length);
            Array.Copy(bytes_data, 0, bytes, bytes_head.Length, bytes_data.Length);

            byte[] result1 = CRC16_1(bytes);
            //byte[] result2 = CRC16_2(bytes);  // not correct
            string byte1 = result1[0].ToString("X");
            string byte2 = result1[1].ToString("X");
            string log = string.Empty;

            //WriteXML3("Events_info.conf");    // just for test
            //ReadXML3();

            //if (null != m_farm)
            //{
            //    //
            //    CDeviceManager devManager = m_farm.DeviceManager;
            //    List<CDeviceBase> devices = devManager.GetAllDevices();
            //    List<CCamera> cameras = devManager.GetAllCameras();
            //    List<CHardwareDevice> hardwareDevices = devManager.GetAllHardwareDevices();
            //    List<CAccessDevice> accessDevices = devManager.GetAllAccessDevices();
            //    //
            //    CPolicies m_policies;
            //    m_policies = new CPolicies(m_farm);
            //    m_policies.Load();
            //    // policy (loc, action, schedule)
            //    DataSet policyData = m_policies.Policies;
            //    IEnumerable<CPolicy> policyList = m_policies.GetPolicies();
            //    IEnumerable < CPolicy> eventList = m_policies.Locations;
            //    IEnumerable<CPolicy> actionList = m_policies.Actions;
            //    IEnumerable<CPolicy> scheduleList = m_policies.Schedules;
            //    CPolicy test_policy = m_policies.GetPolicy(29);
            //    CPolicy test_event = m_policies.GetLocation(18);
            //    CPolicy test_action = m_policies.GetAction(16);
            //    CPolicy test_schedule = m_policies.GetSchedule(17);
            //    foreach (CPolicy policy in policyList)
            //    {
            //        int policyId = policy.getId();
            //        string policyXML = policy.getXML();
            //        bool isEnabled = !policy.IsManualAlarmPolicy();
            //        int policyOrderNum = (int)policy.getOrder();
            //        string policyName = policy.getName();
            //        string policyType = policy.getType();
            //        // policy XML
            //        CPolicy action = policy.GetAction();
            //        int actionId = policy.GetActionID();
            //        string actionName = policy.GetActionName();
            //        CPolicy schedule = policy.GetSchedule();
            //        int scheduleId = policy.GetScheduleID();
            //        string scheduleName = policy.GetScheduleName();
            //        int priority = int.Parse(policy.getValue("priority"));
            //        string eventXML = policy.getLocationsXML();
            //        List<int> locIdList = policy.GetLocationID();
            //        string locName = policy.GetLocationName();
            //        foreach (int locId in locIdList)
            //        {
            //            if (locId < 0)
            //            {
            //                continue;
            //            }

            //            CPolicy location = m_policies.GetLocation(locId);
            //            if (null == location)
            //            {
            //                continue;
            //            }
            //            XmlDocument doc = new XmlDocument();
            //            doc.LoadXml(location.getXML());
            //            XmlNode locNode = doc.SelectSingleNode("location");
            //            XmlNodeList nodes = locNode.ChildNodes;
            //        }
            //    }
            //}

            //ServerConnectionManager scm = null;
            //scm = ServerConnectionManager.CreateManager(Utils.ToEndPoints("127.0.0.1"), new Credentials(User, Password));
            //using (Signals ws = scm.GetWebServiceProxy<Signals>())
            //{
            //    //ws.WallCreateMultiViewPanel2(User, Password, "192.168.77.244:50005", new PanelDisplayType[] { PanelDisplayType.BlankDisplay, PanelDisplayType.Jpeg, PanelDisplayType.HistoricalVideo, PanelDisplayType.LiveVideo }, new uint[] { 1, 0, 2, 6 }, 
            //    //    new VideoRenderModes[] { VideoRenderModes.VR_DEFAULT, VideoRenderModes.VR_ACTUALSIZE, VideoRenderModes.VR_MAINTAINASPECTRATIO, VideoRenderModes.VR_FITTOWINDOW},
            //    //    new long[] { 0, 0, 0, 0 }, 0, 0, 0);

            //    //int ret;
            //    //string name;
            //    //// throw exception if there's no camera in the panel
            //    //ws.WallGetPanelsAsync("Bazzi_Video Wall");
            //    //string[] panels = ws.WallGetPanels("Bazzi_Video Wall"); // each item "panel_name, camera_id"
            //    //ret = ws.WallGetCameraNameOnPanel("Bazzi_Video Wall", "CN-DESKTOPIREN:50005-MV Console-P1", out name);
            //    //ret = ws.WallGetCameraNameOnPanel("Bazzi_Video Wall", "CN-DESKTOPIREN:50005-MV Console-P2", out name);
            //    //ret = ws.WallGetCameraNameOnPanel("Bazzi_Video Wall", "CN-DESKTOPIREN:50005-MV Console-P3", out name);
            //    //ret = ws.WallGetCameraNameOnPanel("Bazzi_Video Wall", "CN-DESKTOPIREN:50005-MV Console-P4", out name);
            //    //byte[] bytesImage = ws.WallPanelScreenshotResized(User, Password, "CN-DESKTOPIREN:50005-MV Console-P1", 1080);
            //    //using (FileStream stream = new FileStream(@"snapshot.jpg", FileMode.Create))
            //    //{
            //    //    stream.Write(bytesImage, 0, bytesImage.Length);
            //    //}
            //}

            //var ipAddresses = Dns.GetHostEntry("192.168.77.244").AddressList.ToList();
            //CVideoWallManager videowallManager = null;
            //videowallManager = CVideoWallManager.GetVideoWallManager(m_farm);
            //videowallManager.Initialize();
            //if (null != videowallManager)
            //{
            //    CVideoWallController videowallController = null;
            //    foreach (CVideoWallController controller in videowallManager.VideoWallControllers)
            //    {
            //        IPAddress ip = IPAddress.Parse(controller.IP);
            //        if (ipAddresses.Contains(ip) || (controller.Name.Equals("192.168.77.244", StringComparison.OrdinalIgnoreCase)))
            //        {
            //            videowallController = controller;
            //            videowallController.Connect();
            //        }
            //    }
            //    if (null != videowallController)
            //    {
            //        foreach (IViewContainer container in videowallController.ViewContainers)
            //        {
            //            foreach (ICameraView view in container.Views)
            //            {
            //                string viewID = view.ID;
            //                byte[] bytesImage = view.GetCurrentScreenshotResized(1080);
            //                using (FileStream stream = new FileStream(@"snapshot.jpg", FileMode.Create))
            //                {
            //                    stream.Write(bytesImage, 0, bytesImage.Length);
            //                }
            //            }
            //        }
            //    }
            //}

            //CUserManagerBase userManager;
            //CVideoWallManagerBase videoWallManager;
            //CDeviceManager deviceManager;
            //CSecurityManager securityManager;
            //ClaimMaker claimMaker;
            //if (null != m_farm)
            //{
            //    userManager = m_farm.UserManager;
            //    videoWallManager = m_farm.VideoWallManager;
            //    deviceManager = m_farm.DeviceManager;
            //    securityManager = m_farm.SecurityManager as CSecurityManager;
            //    int profileId = securityManager.ActiveSecurityProfile.ID;
            //    claimMaker = new ClaimMaker(securityManager, profileId);

            //    CUserBase[] users = userManager.GetUserList();
            //    List<CCamera> cameras = deviceManager.GetAllCameras();
            //    CVideoWallModelBase[] walls = videoWallManager.GetMembers(videoWallManager.RootGroup, false);
            //    foreach (CVideoWallBase wall in walls)
            //    {
            //        string wallName = wall.Name;
            //    }
            //}

            //foreach (CCamera cam in m_farm.DeviceManager.GetAllCameras())
            //{
            //    CResolution resolution = cam.Resolution;
            //    string url = cam.RtspUrl;
            //    CCameraSettings settings = cam.Settings;
            //    CCamera.ELensType lens_type = cam.GetLensType();
            //    bool isPTZCam = cam.IsPTZCamera;
            //    bool isPTZ = cam.IsPTZ();
            //    if (isPTZCam && isPTZ)
            //    {
            //        //bool isDone = false;
            //        //// x speed - for left and + for right
            //        //// y speed - for up and + for down
            //        //// z speed - for wide and + for tele -> seem not work
            //        //isDone = cam.BeginMoveCamera(20, 0, 0, 20, 1, false);
            //        //isDone = cam.EndMoveCamera(20, 2);
            //        //// z speed - for wide and + for tele
            //        //isDone = cam.BeginZoomCamera(-20, 20, 3, 1);    // rapidClickPeriod?
            //        //isDone = cam.EndMoveCamera(20, 4);

            //        //cam.MoveCameraXYZ(20, 20, 20, 20, 5, false);
            //        //cam.EndMoveCamera(20, 6);

            //        //// MOVE_LEFT/MOVE_RIGHT/MOVE_UP/MOVE_DOWN not work in MoveCameraSpecial
            //        //// FOCUS1 for focus near and FOCUS2 for focus far
            //        //cam.MoveCameraSpecial(CCamera.FOCUS1);
            //        //cam.MoveCameraSpecial(CCamera.FOCUS2);

            //        //cam.PTZMoveSpecial(CCamera.FOCUS1); // focus+
            //        //cam.PTZMoveSpecial(CCamera.FOCUS2); // focus-
            //        //cam.PTZMoveSpecial(CCamera.BRIGHT1);    // brightness+
            //        //cam.PTZMoveSpecial(CCamera.BRIGHT2);    // brightness-
            //        //cam.PTZMoveSpecial(CCamera.IRIS1);  // iris+
            //        //cam.PTZMoveSpecial(CCamera.IRIS2);  // iris-
            //        //cam.PTZMoveSpecial(CCamera.CONTRAST1);  // contrast+
            //        //cam.PTZMoveSpecial(CCamera.CONTRAST2);  // contrast-
            //    }
            //}

            //// fail to export video due to dll maybe
            //List<CCamera> cameras = m_farm.DeviceManager.GetAllCameras();
            //if (0<cameras.Count)
            //{
            //    CCamera camera = cameras[0];
            //    if (null != camera)
            //    {
            //        uint number = camera.NumberOfStreams;
            //        CCamera.ELensType lens_type = camera.GetLensType();
            //        if ((0 != number) && (CCamera.ELensType.BASIC_LENS == lens_type))
            //        {
            //            CStream stream = camera.GetStream(number);
            //            if (null != stream)
            //            {
            //                CFisheyeReference fisheyeRef = null;
            //                bool enableWatermark = false;
            //                using (Signals proxy = m_farm.Signals)
            //                {
            //                    try
            //                    {
            //                        enableWatermark = bool.Parse(proxy.GetSettingPair(m_farm.User, m_farm.Password, ShConst.CameraSettingType, camera.CameraId, ShConst.CameraCameraType, ShConst.CameraEnableWatermark, "false"));
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        AILog.Log(ex);
            //                    }
            //                }

            //                DateTime dtStartTime = Convert.ToDateTime("2018-09-27 17:00:00");
            //                DateTime dtEndTime = Convert.ToDateTime("2018-09-27 17:30:00");
            //                DateTimeRange exportRange = new DateTimeRange(dtStartTime.ToUniversalTime(), dtEndTime.ToUniversalTime(), dtEndTime - dtStartTime);
            //                string paramString = camera.VideoParamString(stream, false, exportRange.Start, false, (int)exportRange.Duration.TotalSeconds);
            //                ExportSource exportSource = new ExportSource(
            //                                        camera.Server.NetworkAddress,
            //                                        camera.Server.RTSPPort,
            //                                        camera.DetectionXML,
            //                                        paramString,
            //                                        camera.VideoOptionsString(paramString, false, true),
            //                                        string.Format("{0} - {1}", camera.Name, stream.ToString()),
            //                                        camera.GetReferenceData(),
            //                                        fisheyeRef,
            //                                        camera.CanAccess(DeviceRight.ViewPrivateVideo),
            //                                        enableWatermark,
            //                                        camera.RotateDegrees,
            //                                        camera.RecordFPS,
            //                                        camera.Bitrate);

            //                CServer server = camera.Server as CServer;
            //                TimeZoneOffset timezone = server.GetTimezoneInformation();
            //                IVideoExporterFactory factory;
            //                CompressorIdentifier[] video_compressors = new CompressorDirectory().GetAvailableVideoCompressors();
            //                string[] audio_compressors = new CompressorDirectory().GetAvailableAudioCompressors();
            //                factory = NetSendHistToAviExporterSelector.GetExporterFactory(
            //                            exportSource,
            //                            exportRange,
            //                            timezone,
            //                            ShConst.DefaultDecorationFontFamily,
            //                            ConvertMBToBytes(short.Parse("640")),
            //                            "E:\\",
            //                            DecorationLevel.None,
            //                            (CompressorIdentifier)video_compressors[0],
            //                            audio_compressors[2]);

            //                ITranslator translator;
            //                TranslatorSingletonFactory singletonFactory = new TranslatorSingletonFactory();
            //                translator = singletonFactory.GetTranslator();
            //                ExportBackgroundWorker worker = new ExportBackgroundWorker(
            //                                                factory,
            //                                                new ExportReportGenerator(camera.FarmUsername, DateTime.Now),
            //                                                new ExportReportWriter(translator, timezone));
            //                worker.Name = exportSource.Name;
            //                worker.ExportCompleted += new EventHandler<AsyncVideoExportCompletedEventArgs>(ExportCompleted);
            //                worker.StartAsync();

            //            }
            //        }
            //    }
            //}

        }

        /// <summary>
        /// Convert from megabytes to bytes.
        /// </summary>
        private uint ConvertMBToBytes(short megaBytes)
        {
            return (uint)(megaBytes * 1024 * 1024);
        }
        /// <summary>
        /// Event that is called when the export has finished
        /// </summary>
        private void ExportCompleted(object sender, AsyncVideoExportCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("The export was cancelled", "Export Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("The export has completed", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void time1_Tick(object sender, EventArgs e)
        {
            this.toolStripStatusLabel1.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (null != m_camera)
            {
                this.toolStripStatusLabel3.Text = "Current Device is " + m_camera.Name;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            for (int i=0; i<lvJobs.Items.Count; i++)
            {
                ListViewItem item = lvJobs.Items[i];
                int count = item.SubItems.Count;
                if (3 == count)
                {
                    ServiceJob job = (ServiceJob)hashJobs[int.Parse(item.SubItems[0].Text)];
                    item.SubItems[2].Text = (job.m_avms == null ? string.Empty : job.m_avms.IsConnected.ToString());
                }
            }
        }


        private void btnImportJob_Click(object sender, EventArgs e)
        {
            if (null != hashJobs)
            {
                return;
            }
            hashJobs = new Hashtable();

            ServiceJob job1 = new ServiceJob();
            job1.m_jobId = 1;
            job1.m_jobName = "AVMS Listener";
            //job1.InitAVMSServer("127.0.0.1", "admin", "admin");
            //job1.JobEventSend += new ServiceJob.JobEventHandler(this.ServiceJob_EventSend);
            job1.FarmConnectedEvent += new EventHandler<EventArgs>(this.Farm_ConnectedEvent);
            hashJobs.Add(job1.m_jobId, job1);
            ServiceJob job2 = new ServiceJob();
            job2.m_jobId = 2;
            job2.m_jobName = "3rd upload AVMS";
            //job2.InitAVMSServer("127.0.0.1", "admin", "admin");
            //job2.JobEventSend += new ServiceJob.JobEventHandler(this.ServiceJob_EventSend);
            job2.FarmConnectedEvent += new EventHandler<EventArgs>(this.Farm_ConnectedEvent);
            hashJobs.Add(job2.m_jobId, job2);

            lvJobs.Items.Clear();
            ListViewItem item1 = new ListViewItem(new string[] { job1.m_jobId.ToString(), job1.m_jobName, job1.m_avms == null ? string.Empty : job1.m_avms.IsConnected.ToString() });
            ListViewItem item2 = new ListViewItem(new string[] { job2.m_jobId.ToString(), job2.m_jobName, job2.m_avms == null ? string.Empty : job2.m_avms.IsConnected.ToString() });
            lvJobs.Items.Add(item1);
            lvJobs.Items.Add(item2);
            for (int i=0; i<lvJobs.Columns.Count; i++)
            {
                lvJobs.Columns[i].Width = -2;   // -1 only for content
            }
            

        }

        private void btnClearJob_Click(object sender, EventArgs e)
        {
            lvJobs.Items.Clear();
            hashJobs = null;
        }

        private void lvJobs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (1 != lvJobs.SelectedItems.Count)
            {
                return;
            }

            int jobId;
            bool bRet = int.TryParse(lvJobs.SelectedItems[0].Text, out jobId);
            if (bRet)
            {
                currentJob = (ServiceJob)hashJobs[jobId];
                //m_bConnectedToAVMSServer = currentJob.m_avms.IsConnected;
                SetConnectButton(!currentJob.m_bConnectedToAVMSServer, currentJob.m_bConnectedToAVMSServer);
                UpdateControls(currentJob.m_bConnectedToAVMSServer);
            }
        }

        private void rbMultiJob_CheckedChanged(object sender, EventArgs e)
        {
            if (rbMultiJob.Checked)
            {
                lvJobs.Enabled = true;
                if (null == currentJob)
                {
                    SetConnectButton(false, false);
                    UpdateControls(false);
                }
                else
                {
                    SetConnectButton(!currentJob.m_bConnectedToAVMSServer, currentJob.m_bConnectedToAVMSServer);
                    UpdateControls(currentJob.m_bConnectedToAVMSServer);
                    lvJobs.Focus();
                }
            }
            else
            {
                lvJobs.Enabled = false;
            }
        }

        private void rbOverall_CheckedChanged(object sender, EventArgs e)
        {
            if (rbOverall.Checked)
            {
                lvJobs.Enabled = false;
                lvJobs.SelectedItems.Clear();
                currentJob = null;
                SetConnectButton(!m_bConnectedToAVMSServer, m_bConnectedToAVMSServer);
                UpdateControls(m_bConnectedToAVMSServer);
            }
        }
    }


}
