using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TC4I
{
    struct AVMS_Policy_Rule
    {
        public int Policy_ID;
        public int Camera_ID;
        public string Event_Name;
    }

    enum Peake_Event
    { Alarm = 0, Invalid, Threat, Secondcard_fail, Passwd_fail, Open_fail, End }

    class PA_xmlConfig
    {
        AVMS_Policy_Rule[] rules;
        public PA_xmlConfig()
        {
            // Initial array
            rules = new AVMS_Policy_Rule[(int)Peake_Event.End + 1];
            for (int i = 0; i < (int)Peake_Event.End + 1; i++)
            {
                rules[i].Policy_ID = -1;
            }
            rules[(int)Peake_Event.Invalid].Event_Name = "无效刷卡";
            rules[(int)Peake_Event.Threat].Event_Name = "胁迫密码开门";
            // Initial end
        }

        public void Load_Config()
        {
            XDocument xd = XDocument.Load("transaction.conf");

            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "rule" && s.Parent.Name.LocalName == "rule_event_map" && s.Attribute("name").Value == "Peake"
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

                for(int i=0;i<(int)Peake_Event.End+1;i++)
                {
                    if (item.Attribute("event").Value == rules[i].Event_Name)
                    {
                        rules[i].Policy_ID = policy_id;
                        rules[i].Camera_ID = camera_id;
                    }
                }
            }
        }
    }
}
