using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

using System.Runtime.InteropServices;

namespace TransactionServer.Jobs.Job2
{
    public class Job : Base.ServiceJob
    {

        //public static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        //public static void ShowMessageBox(string message, string title)
        //{
        //    int resp = 0;
        //    WTSSendMessage(
        //        WTS_CURRENT_SERVER_HANDLE,
        //        WTSGetActiveConsoleSessionId(),
        //        title, title.Length,
        //        message, message.Length,
        //        0, 0, out resp, false);
        //}

        //[DllImport("kernel32.dll", SetLastError = true)]
        //public static extern int WTSGetActiveConsoleSessionId();

        //[DllImport("wtsapi32.dll", SetLastError = true)]
        //public static extern bool WTSSendMessage(
        //    IntPtr hServer,
        //    int SessionId,
        //    String pTitle,
        //    int TitleLength,
        //    String pMessage,
        //    int MessageLength,
        //    int Style,
        //    int Timeout,
        //    out int pResponse,
        //    bool bWait);

        protected override void Init()
        {
            // throw new NotImplementedException();
            //ShowMessageBox("This a message from AlertService.","AlertService Message");
        }

        protected override void Cleanup()
        {
            //throw new NotImplementedException();
        }

        protected override void Start()
        {
            try
            {
                this.m_IsRunning = true;
                this.executeLogic();
            }
            catch (Exception error)
            {
                //
            }
            finally
            {
                //
            }
        }

        protected override void Stop()
        {
            this.m_IsRunning = false;
        }

        private void executeLogic()
        {
            //
        }
    }
}
