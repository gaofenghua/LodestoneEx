using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TransactionServer.Jobs.ACAPServer
{
    public enum CONFIG_TYPE
    {
        REGISTRATION = 0,
        DEVICEINFO = 1,
    }

    public class RegInfo
    {
        public string m_deviceId { get; private set; } = string.Empty;
        public string m_username { get; private set; } = string.Empty;
        public string m_password { get; private set; } = string.Empty;
        public string m_defaultUsername { get; private set; } = string.Empty;
        public string m_defaultPassword { get; private set; } = string.Empty;

        public RegInfo(string defaultUser, string defaultPassword)
        {
            m_defaultUsername = defaultUser;
            m_defaultPassword = defaultPassword;
        }

        public void SetDefaultUserpwd()
        {
            SetUserpwd(m_defaultUsername, m_defaultPassword);
        }

        public void SetUserpwd(string user, string password)
        {
            m_username = user;
            m_password = password;
        }

        public bool Process(string[] data)
        {
            string log = string.Empty;

            // 12345678901234567890,root:pass
            if (2 != data.Length)
            {
                log += "Undefined data structure refer to \"id,user:password\", stop parsing\n";
                PrintLog(log);
                return false;
            }

            if (string.Empty == data[0])
            {
                log += String.Format("Empty value for deviceId, stop parsing\n");
                PrintLog(log);
                return false;
            }
            this.m_deviceId = data[0];
            log += String.Format("deviceId = {0}\n", this.m_deviceId);

            if (string.Empty == data[1])
            {
                SetDefaultUserpwd();
                log += String.Format("Empty value for userpwd, replace with default value - {0}:{1}\n", this.m_username, this.m_password);
            }
            else if (2 == data[1].Split(':').Length)
            {
                this.m_username = data[1].Split(':')[0];
                this.m_password = data[1].Split(':')[1];
                log += String.Format("user = {0}, password = {1}\n", this.m_username, this.m_password);
            }
            else
            {
                this.m_username = string.Empty;
                this.m_password = string.Empty;
                log += String.Format("Invalid value for userpwd, replace with empty value - {0}:{1}\n", this.m_username, this.m_password);
            }

            PrintLog(log);
            return true;
        }

        private void PrintLog(string text)
        {
            Trace.WriteLine(text);
        }
    }

    public class APEInfo
    {
        public string m_deviceId { get; private set; } = string.Empty;
        public string m_deviceIp { get; private set; } = string.Empty;
        public ACAP_TYPE m_acapType { get; private set; } = ACAP_TYPE.ACAP_TYPE_UNKNOWN;

        public void SetDefaultACAPType()
        {
            SetACAPType(ACAP_TYPE.ACAP_TYPE_UNKNOWN);
        }

        public void SetACAPType(ACAP_TYPE type)
        {
            m_acapType = type;
        }

        public bool Process(string[] data)
        {
            string log = string.Empty;

            // 12345678901234567890,192.168.77.173,1
            if (3 > data.Length)
            {
                log += "Undefined data structure refer to \"id,ip,type,...\", stop parsing\n";
                PrintLog(log);
                return false;
            }

            if (string.Empty == data[0])
            {
                log += String.Format("Empty value for deviceId, stop parsing\n");
                PrintLog(log);
                return false;
            }
            this.m_deviceId = data[0];
            log += String.Format("deviceId = {0}\n", this.m_deviceId);

            if (!ValidateIPAddress(data[1]))
            {
                log += String.Format("Invalid value for deviceIp - {0}\n", data[1]);
                PrintLog(log);
                return false;
            }
            this.m_deviceIp = data[1];
            log += String.Format("deviceIp = {0} with\n", data[1]);

            int type_id;
            ACAP_TYPE type;
            if (int.TryParse(data[2], out type_id))
            {
                type = (ACAP_TYPE)type_id;
                if (!ValidateEnumType(type))
                {
                    SetDefaultACAPType();
                    log += String.Format("Invalid value for acapType - {0}, replace with default value - {1}\n", type, this.m_acapType);
                }
                else
                {
                    this.m_acapType = type;
                    log += String.Format("acapType = {0}\n", this.m_acapType);
                }
            }
            else if (Enum.TryParse<ACAP_TYPE>(data[2], out type))
            {
                this.m_acapType = type;
                log += String.Format("acapType = {0}\n", this.m_acapType);
            }
            else
            {
                SetDefaultACAPType();
                log += String.Format("Invalid value for acapType - {0}, replace with default value - {1}\n", data[2], this.m_acapType);
            }
            PrintLog(log);
            return true;
        }

        private void PrintLog(string text)
        {
            Trace.WriteLine(text);
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
