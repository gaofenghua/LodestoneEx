using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using TransactionServer.Jobs.Peake_Access;
using System.Net;

namespace TC4I
{
    //Policy Rules
    struct AVMS_Policy_Rule
    {
        public int Policy_ID;
        public int Camera_ID;
    }

    struct PA_Controller
    {
        public int ID;
        public string IP;
        public int Port;
        public AVMS_Policy_Rule[,] Rules;
    }

    struct Event_Map
    {
        public string Event_Name;
        public Peake_Event[] Events;
    }
    class PA_xmlConfig
    {
        public List<PA_Controller> Controllers;
        public bool status;
        public string message;

        public Event_Map[] event_map;
        public PA_xmlConfig()
        {
            Controllers = new List<PA_Controller>();

            status = true;
            message = "";
        }
    
        public void Load_Event_Map()
        {
            string file = System.Windows.Forms.Application.StartupPath.ToString() + @"\" + "transaction.conf";
            if (File.Exists(file) == false)
            {
                message = String.Format("{0} does not exist.", file);
                status = false;
                return;
            }

            XDocument xd;
            try
            {
                xd = XDocument.Load(file);
            }
            catch(Exception e)
            {
                message = e.Message;
                status = false;
                return;
            }
            
            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "event" && s.Parent.Name.LocalName == "event_map" && s.Parent.Attribute("owner") != null && s.Parent.Attribute("owner").Value == "Peake"
                        select s;

            int nQuery = query.Count();
            event_map = new Event_Map[nQuery];

            int i = 0;
            foreach (XElement item in query)
            {
                event_map[i].Event_Name = item.Value;

                string pa_eventsName="";
                if(null != item.Attribute("desc"))
                {
                    pa_eventsName = item.Attribute("desc").Value;
                }
                string[] words = pa_eventsName.Split(',');
                int nWords = words.Count();
                event_map[i].Events = new Peake_Event[nWords];
                int j = 0;
                bool bFound = false;
                foreach (var word in words)
                {
                    bFound = false; 
                    for(int k=0;k<(int)Peake_Event.End;k++)
                    {
                        if(Peake_Access.Event_Name[k] == word)
                        {
                            event_map[i].Events[j] = (Peake_Event)k;
                            bFound = true;
                            break;
                        }
                    }

                    if(false == bFound)
                    {
                        event_map[i].Events[j] = Peake_Event.End;
                        message = string.Format("warning: {0} Non supported event in Peake Access System",word);
                    }
                    j = j + 1;
                }

                i = i + 1;
            }

            Load_Policy_Map(xd);
            Check_Configuration();
        }

        public void Load_Policy_Map(XDocument xd)
        {
            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "policy" && s.Parent.Name.LocalName == "policy_map" && s.Parent.Attribute("owner") != null && s.Parent.Attribute("owner").Value == "Peake"
                        select s;
            foreach (XElement item in query)
            {
                int policyID = -1;
                int cameraID = -1;
                string ip = "";
                int port = -1;
                string eventName = "";
                int doorNumber = 0;
                try
                {
                    policyID = Int32.Parse(item.Value);
                    cameraID = Int32.Parse(item.Attribute("camId").Value);
                    ip = item.Attribute("devIp").Value;
                    port = Int32.Parse(item.Attribute("devPort").Value);
                    eventName = item.Attribute("event").Value;
                    if (null != item.Attribute("door"))
                    {
                        doorNumber = Int32.Parse(item.Attribute("door").Value);
                    }

                    if(doorNumber<0 || doorNumber > 8)
                    {
                        throw new IndexOutOfRangeException("The door number is out of range");
                    }
                    //Check for ip address format
                    IPAddress ipaddress;
                    if(IPAddress.TryParse(ip,out ipaddress)==false)
                    {
                        throw new IndexOutOfRangeException("The IP address is incorrect");
                    }
                }
                catch (Exception e) //NullReferenceException,FormatException
                {
                    if (message != "")
                    {
                        message += "\r\n";
                    }
                    message += string.Format("warning: Peake_Access Load Policy map issue, {0}, policy={1}, camId={2}, devIp={3}, devPort={4}, event={5}, door={6}.",
                        e.Message, policyID, cameraID,ip,port,eventName,doorNumber);
                    continue;
                }

                Add_Control_Rule(ip, port, policyID, cameraID, doorNumber, eventName);
            }
        }

        public void Check_Configuration()
        {
            if(Controllers.Count() > Peake_Access.Maximum_Controller_Number)
            {
                Peake_Access.PrintLog(0, String.Format("error: Maximum controller number exceeded. Maximum = {0}, Current controller = {1}. ", Peake_Access.Maximum_Controller_Number,Controllers.Count()));
                status = false;
            }
            status = true;
        }
        public void Add_Control_Rule(string ip, int port,int policyID, int cameraID, int doorNumber, string eventName)
        {
            Peake_Event[] paEvents = Get_Events(eventName);
            if (paEvents == null)
            {
                Peake_Access.PrintLog(0, String.Format("warning: configuration load issue, Event: {0} does not exist. ", eventName));
                return;
            }

            int nController = Controllers.Count();

            int found = -1;
            for(int i=0;i<nController;i++)
            {
                if(Controllers[i].IP == ip)
                {
                    found = i;
                    break;
                }
            }

            if(-1 != found)
            {
                for(int i=0;i<paEvents.Count();i++)
                {
                    int eventIndex = (int)paEvents[i];
                    // Avoid double setup for one event/door
                    if (Controllers[found].Rules[doorNumber, eventIndex].Policy_ID != -1 && Controllers[found].Rules[doorNumber, eventIndex].Camera_ID != -1)
                    {
                        Peake_Access.PrintLog(0, String.Format("warning: configuration override, Controller={0} door={1} event={2}, Policy already exist ({3} -> {4}). ", Controllers[found].IP, doorNumber, eventName, Controllers[found].Rules[doorNumber, eventIndex].Policy_ID,policyID));
                    }
                    Controllers[found].Rules[doorNumber, eventIndex].Policy_ID = policyID;
                    Controllers[found].Rules[doorNumber, eventIndex].Camera_ID = cameraID;
                }
            }
            else
            {
                // Initial array
                AVMS_Policy_Rule[,] rules = new AVMS_Policy_Rule[Peake_Access.Door_Number + 1, (int)Peake_Event.End + 1];

                for (int j = 0; j < Peake_Access.Door_Number + 1; j++)
                {
                    for (int i = 0; i < (int)Peake_Event.End + 1; i++)
                    {
                        rules[j, i].Policy_ID = -1;
                        rules[j, i].Camera_ID = -1;
                    }
                }
                // Initial end

                PA_Controller controller = new PA_Controller();
                controller.ID = nController + 1;
                controller.IP = ip;
                controller.Port = port;
                controller.Rules = rules;
                for (int i = 0; i < paEvents.Count(); i++)
                {
                    int eventIndex = (int)paEvents[i];
                    controller.Rules[doorNumber, eventIndex].Policy_ID = policyID;
                    controller.Rules[doorNumber, eventIndex].Camera_ID = cameraID;
                }

                Controllers.Add(controller);
            }
        }

        public Peake_Event[] Get_Events(string eventName)
        {
            foreach(Event_Map paEvent in event_map)
            {
                if(paEvent.Event_Name == eventName)
                {
                    return paEvent.Events;
                }
            }
            return null;
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
    }
}
