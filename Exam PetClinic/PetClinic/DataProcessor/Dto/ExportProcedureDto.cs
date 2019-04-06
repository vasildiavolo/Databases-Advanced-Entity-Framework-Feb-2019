using System.Collections.Generic;
using System.Xml.Serialization;

namespace PetClinic.DataProcessor.Dto
{
    [XmlType("Procedure")]
    public class ExportProcedureDto
    {
        public string Passport { get; set; }

        public string OwnerNumber { get; set; }

        public string DateTime { get; set; }

        [XmlArray("AnimalAids")]
        public List<ExportAnimalAid> AnimalAids { get; set; } = new List<ExportAnimalAid>();

        [XmlElement("TotalPrice")]
        public decimal TotalPrice { get; set; }

    }
}
