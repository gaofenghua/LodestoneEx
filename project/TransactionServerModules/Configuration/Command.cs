using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

namespace TransactionServerModules.Configuration
{
    [XmlType(TypeName = "command")]
    public class Command
    {
        [XmlAttribute("id")]
        public string command_id { get; set; }

        [XmlAttribute("desc")]
        public string command_desc { get; set; }

        [XmlElement("method")]
        public string command_method { get; set; }

        [XmlElement("url")]
        public string command_url { get; set; }

        [XmlElement("body")]
        public string command_body { get; set; }

        [XmlElement("body_elements", typeof(BodyElement))]
        //public ArrayList body_elements = new ArrayList(); // there's only one body_elements node
        public BodyElement body_elements { get; set; }
    }
}
