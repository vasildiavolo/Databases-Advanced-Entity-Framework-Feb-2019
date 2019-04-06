using System.Xml.Serialization;

namespace PetClinic.DataProcessor.Dto
{
    [XmlType("AnimalAid")]
    public class ExportAnimalAid
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Price")]
        public decimal Price { get; set; }
    }
}
