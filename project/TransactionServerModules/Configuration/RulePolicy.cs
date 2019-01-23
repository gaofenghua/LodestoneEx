using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

namespace TransactionServerModules.Configuration
{
    [XmlType(TypeName = "rule")]
    public class RulePolicy
    {
        [XmlAttribute("id")]
        public string rule_id { get; set; }

        [XmlAttribute("name")]
        public string rule_name { get; set; }

        [XmlText]
        public string event_id { get; set; }    // separated by ','
    }
}
