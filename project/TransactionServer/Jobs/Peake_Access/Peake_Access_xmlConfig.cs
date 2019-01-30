using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace TC4I
{
    //Policy Rules
    struct AVMS_Policy_Rule
    {
        public int Policy_ID;
        public int Camera_ID;
        public string Event_Name;
    }

    struct PA_Controller
    {
        public int ID;
        public string IP;
        public int Port;
        public AVMS_Policy_Rule[] Rules;
    }
    enum Peake_Event
    { Alarm = 0, Invalid, Threat, Secondcard_fail, Passwd_fail, Open_fail, End }

    class PA_xmlConfig
    {
        public List<PA_Controller> Controllers;
        //public AVMS_Policy_Rule[] rules;
        public bool status;
        public string message;
        public PA_xmlConfig()
        {
            //// Initial array
            //rules = new AVMS_Policy_Rule[(int)Peake_Event.End + 1];
            //for (int i = 0; i < (int)Peake_Event.End + 1; i++)
            //{
            //    rules[i].Policy_ID = -1;
            //}
            //rules[(int)Peake_Event.Invalid].Event_Name = "无效刷卡";
            //rules[(int)Peake_Event.Threat].Event_Name = "胁迫密码开门";
            //// Initial end

            Controllers = new List<PA_Controller>();

            status = false;
            message = "";
        }

        public void Load_Systems()
        {
            string file = System.Windows.Forms.Application.StartupPath.ToString() + @"\" + "transaction.conf";
            if (File.Exists(file) == false)
            {
                message = String.Format("{0} does not exist.", file);
                status = false;
                return;
            }


            XDocument xd = XDocument.Load(file);

            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "Peake_Controller" && s.Parent.Name.LocalName == "system" 
                        select s;

            foreach (XElement item in query)
            {
                int id = -1;
                string ip = "";
                int port = -1;
                try
                {
                    id = Int32.Parse(item.Value);
                    ip = item.Attribute("ip").Value;
                    port = Int32.Parse(item.Attribute("port").Value);
                }
                catch (Exception e) //NullReferenceException,FormatException
                {
                    //MessageBox.Show(e.Message);
                    break;
                }

                if(id != -1 && ip != "" && port!= -1)
                {
                    PA_Controller con = new PA_Controller();
                    con.ID = id;
                    con.IP = ip;
                    con.Port = port;

                    con.Rules = Load_Rules(xd, con.ID);
                    if(con.Rules != null)
                    {
                        Controllers.Add(con);
                        status = true;
                    }
                    else
                    {
                        if(message != "")
                        {
                            message += "\r\n";
                        }
                        message += string.Format("warning: There's no rules for Peake_Access controller {0} id={1}.", con.IP, con.ID);

                    }
                    
                }
               
            }

        }

        public AVMS_Policy_Rule[] Load_Rules(XDocument xd, int controller_id)
        {
            // Initial array
            AVMS_Policy_Rule[] rules = new AVMS_Policy_Rule[(int)Peake_Event.End + 1];
            for (int i = 0; i < (int)Peake_Event.End + 1; i++)
            {
                rules[i].Policy_ID = -1;
            }
            rules[(int)Peake_Event.Invalid].Event_Name = "无效刷卡";
            rules[(int)Peake_Event.Threat].Event_Name = "胁迫密码开门";
            // Initial end

            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "rule" && s.Parent.Name.LocalName == "rule_event_map" 
                        && s.Attribute("name") != null && s.Attribute("name").Value == "Peake" 
                        && s.Attribute("controller") != null && s.Attribute("controller").Value == controller_id.ToString()
                        select s;

            foreach (XElement item in query)
            {
                int policy_id = -1;
                int camera_id = -1;
                try
                {
                    policy_id = Int32.Parse(item.Value);
                    camera_id = Int32.Parse(item.Attribute("Camera_ID").Value);
                }
                catch (Exception e) //NullReferenceException,FormatException
                {
                    //MessageBox.Show(e.Message);
                    break;
                }

                for (int i = 0; i < (int)Peake_Event.End + 1; i++)
                {
                    if (item.Attribute("event").Value == rules[i].Event_Name)
                    {
                        rules[i].Policy_ID = policy_id;
                        rules[i].Camera_ID = camera_id;
                    }
                }
            }

            //Check to see if there's any configuration settled.
            for (int i = 0; i < (int)Peake_Event.End + 1; i++)
            {
                if (rules[i].Policy_ID != -1 && rules[i].Camera_ID != -1)
                {
                    return rules;
                }
            }
            return null;
        }



        //public void Load_Config()
        //{
        //    string file = System.Windows.Forms.Application.StartupPath.ToString() + @"\" + "transaction.conf";
        //    if(File.Exists(file)==false)
        //    {
        //        message = String.Format("{0} does not exist.", file);
        //        status = false;
        //        return;
        //    }
            

        //    XDocument xd = XDocument.Load(file);
           
        //    var query = from s in xd.Descendants()
        //                where s.Name.LocalName == "rule" && s.Parent.Name.LocalName == "rule_event_map" && s.Attribute("name").Value == "Peake"
        //                select s;

        //    foreach (XElement item in query)
        //    {
        //        int policy_id = -1;
        //        int camera_id = -1;
        //        try
        //        {
        //            policy_id = Int32.Parse(item.Value);
        //            camera_id = Int32.Parse(item.Attribute("Camera_ID").Value);
        //        }
        //        catch (Exception e) //NullReferenceException,FormatException
        //        {
        //            //MessageBox.Show(e.Message);
        //            break;
        //        }

        //        for(int i=0;i<(int)Peake_Event.End+1;i++)
        //        {
        //            if (item.Attribute("event").Value == rules[i].Event_Name)
        //            {
        //                rules[i].Policy_ID = policy_id;
        //                rules[i].Camera_ID = camera_id;
        //            }
        //        }
        //    }

        //    //Check to see if there's any configuration settled.
        //    for (int i = 0; i < (int)Peake_Event.End + 1; i++)
        //    {
        //        if(rules[i].Policy_ID != -1 && rules[i].Camera_ID != -1)
        //        {
        //            status = true;
        //            return;
        //        }
        //    }
        //    status = false;
        //    message = "No rule configged for Peake_Access.";
        //    return;
        //}
    }
}
