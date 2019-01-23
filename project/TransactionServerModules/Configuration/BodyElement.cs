using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

namespace TransactionServerModules.Configuration
{
    [XmlType(TypeName = "body_elements")]
    public class BodyElement
    {
        [XmlElement("execute_type")]
        public string execute_type { get; set; }

        [XmlElement("door_id")]
        public string door_id { get; set; }
    }
}
