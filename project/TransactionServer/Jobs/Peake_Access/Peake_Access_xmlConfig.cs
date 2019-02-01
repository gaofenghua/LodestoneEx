using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using TransactionServer.Jobs.Peake_Access;

namespace TC4I
{
    //Policy Rules
    struct AVMS_Policy_Rule
    {
        public int Policy_ID;
        public int Camera_ID;
       // public string Event_Name;
    }

    struct PA_Controller
    {
        public int ID;
        public string IP;
        public int Port;
        public AVMS_Policy_Rule[,] Rules;
    }
    

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
                    if (message != "")
                    {
                        message += "\r\n";
                    }
                    message += string.Format("warning: Peake_Access Load system issue, controller id={0}, ip={1}, port={2}.", id,ip,port);
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

        public AVMS_Policy_Rule[,] Load_Rules(XDocument xd, int controller_id)
        {
            // Initial array
            AVMS_Policy_Rule[,] rules = new AVMS_Policy_Rule[Peake_Access.Door_Number+1,(int)Peake_Event.End + 1];

            for(int j=0;j<Peake_Access.Door_Number+1;j++)
            {
                for (int i = 0; i < (int)Peake_Event.End + 1; i++)
                {
                    rules[j, i].Policy_ID = -1;
                    rules[j, i].Camera_ID = -1;
                }
            }
            // Initial end

            int policy_id = -1;
            int camera_id = -1;
            int door_id = 0;

            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "rule" && s.Parent.Name.LocalName == "rule_event_map" 
                        && s.Attribute("name") != null && s.Attribute("name").Value == "Peake" 
                        && s.Attribute("controller") != null && s.Attribute("controller").Value == controller_id.ToString()
                        select s;

            foreach (XElement item in query)
            {
                policy_id = -1;
                camera_id = -1;
                door_id = 0;
                try
                {
                    policy_id = Int32.Parse(item.Value);
                    camera_id = Int32.Parse(item.Attribute("Camera_ID").Value);

                    if(item.Attribute("door")!=null)
                    {
                        door_id = Int32.Parse(item.Attribute("door").Value);

                        if(door_id < 1 || door_id > Peake_Access.Door_Number)
                        {
                            if (message != "")
                            {
                                message += "\r\n";
                            }
                            message += string.Format("warning: Config door={0} incorrect, controller id={1}.", door_id,controller_id);
                            break;
                        }
                    }
                }
                catch (Exception e) //NullReferenceException,FormatException
                {
                    if (message != "")
                    {
                        message += "\r\n";
                    }
                    message += string.Format("warning: Config exception {0}.", e.Message);

                    break;
                }

                for (int i = 0; i < (int)Peake_Event.End; i++)
                {
                    if (item.Attribute("event").Value == Peake_Access.Event_Name[i])
                    {
                        if(i == 0 || i==1) // 0 和 1 是控制器报警事件，忽略配置文件中对“door”参数的设置
                        {
                            door_id = 0; 
                        }
                        rules[door_id,i].Policy_ID = policy_id;
                        rules[door_id,i].Camera_ID = camera_id;
                    }
                }
            }

            ////------Check 
            //if (controller_id == 1)
            //{
            //    string log = String.Format("\r\n\r\nConfig: id={0}", controller_id);
            //    Peake_Access.PrintLog(log);

            //    for (int i = 0; i < Peake_Access.Door_Number + 1; i++)
            //    {
            //        for (int j = 0; j < (int)Peake_Event.End; j++)
            //        {
            //            log = String.Format("rules[{0},{1}] policy={2}, camera={3} door={0}, event={4}, event_name={5}. ", i, j, rules[i, j].Policy_ID, rules[i, j].Camera_ID, j, Peake_Access.Event_Name[j]);
            //            Peake_Access.PrintLog(log);
            //        }
            //    }
            //}
            ////------end Check

            //Check to see if there's any configuration settled.
            for (int j=0;j<Peake_Access.Door_Number+1;j++)
            {
                for (int i = 0; i < (int)Peake_Event.End; i++)
                {
                    if (rules[j, i].Policy_ID != -1 && rules[j, i].Camera_ID != -1)
                    {
                        return rules;
                    }
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
