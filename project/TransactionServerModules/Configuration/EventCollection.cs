using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

namespace TransactionServerModules.Configuration
{
    [XmlType(TypeName = "configuration")]
    public class EventCollection
    {
        [XmlArray("rule_event_map")]
        public RulePolicy[] RuleList { get; set; }

        [XmlArray("events")]
        public Event[] EventList { get; set; }
    }
}
