using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;
using System.Collections;

namespace TransactionServerModules.Configuration
{
    [XmlType(TypeName = "event")]
    public class Event
    {
        [XmlAttribute("id")]
        public string event_id { get; set; }

        [XmlAttribute("name")]
        public string event_name { get; set; }

        [XmlArray("actions"), XmlArrayItem("action", typeof(Action))]
        public ArrayList actions = new ArrayList();
    }
}
