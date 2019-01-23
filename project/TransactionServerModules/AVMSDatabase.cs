using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Seer.Database;
using System.Data.Odbc;
using System.Data.Common;
using System.Globalization;


namespace TransactionServerModules
{
    class AVMSDatabase
    {
        //private static DbConnection m_conn = null;

        public static string Connect()
        {
            try
            {
                using (var myConn = VmsDatabase.CreateConnection())
                using (var cmd = myConn.CreateCommand("SELECT ID, V FROM Settings WHERE K='CameraTable'"))
                {

                    cmd.CommandTimeout = 10000;
                    myConn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        //m_cameraNames = new Dictionary<int, string>();
                        //while (reader.Read())
                        //{
                        //    int id = int.Parse(reader["ID"].ToString(), CultureInfo.InvariantCulture);
                        //    foreach (string sValue in reader["V"].ToString().Split(';'))
                        //        if (sValue.StartsWith("nm="))
                        //            m_cameraNames[id] = sValue.Substring(3);
                        //}
                        reader.Close();
                    }
                    myConn.Close();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return "Failed to connect to avms database : " + ex.Message;
            }
        }


        public static string Statement { get; set; }
    }
}
