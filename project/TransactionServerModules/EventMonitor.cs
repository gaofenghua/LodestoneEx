using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Seer.BaseLibCS;
using Seer.FarmLib.Client;
using Seer.SDK;
using Seer.SDK.NotificationMonitors;

namespace TransactionServerModules
{
    /// <summary>
    /// Monitor a farm for event messages.
    /// such as Access Control event and so on
    /// </summary>
    public class EventMonitor : CameraMessageMonitor
    {
        #region Member Variables

        /// <summary>
        /// Raised whenever an alarm message is received.
        /// </summary>
        public event EventHandler<EventMessageEventArgs> EventReceived = delegate { };

        #endregion

        #region Constructors

        public EventMonitor(INotificationSource source) : base(source)
        {
        }

        #endregion

        #region Methods

        private bool IsEvent(CameraMessageStruct cameraMessageStruct)
        {
            return cameraMessageStruct.m_iEvent == (uint)T_ALARM_EVENTS.E_SWIPE_ACCESS_GRANTED ||
                cameraMessageStruct.m_iEvent == (uint)T_ALARM_EVENTS.E_SWIPE_ACCESS_DENIED ||
                cameraMessageStruct.m_iEvent == (uint)T_ALARM_EVENTS.E_ACCESS_OTHER ||
                cameraMessageStruct.m_iEvent == (uint)T_ALARM_EVENTS.E_ZOOM_START ||
                cameraMessageStruct.m_iEvent == (uint)T_ALARM_EVENTS.E_ZOOM_DONE;
        }

        #endregion

        #region Event Handlers

        protected override void OnCameraMessageReceived(object sender, CameraMessageStructEventArgs e)
        {
            base.OnCameraMessageReceived(sender, e);

            if (IsEvent(e.Message))
            {
                OnAlarmMessageReceived(sender, new EventMessageEventArgs(e.Source, e.Message));
            }
        }

        protected virtual void OnAlarmMessageReceived(object sender, EventMessageEventArgs e)
        {
            EventReceived(sender, e);
        }

        #endregion
    }

    public class EventMessageEventArgs : CameraMessageStructEventArgs
    {
        #region Constructors

        public EventMessageEventArgs(CServer sourceServer, CameraMessageStruct messageStruct)
            : base(sourceServer, messageStruct)
        {
        }

        #endregion
    }
}
