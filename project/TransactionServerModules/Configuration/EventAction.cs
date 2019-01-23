using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

namespace TransactionServerModules.Configuration
{
    [XmlType(TypeName = "action")]
    public class EventAction
    {
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
}
