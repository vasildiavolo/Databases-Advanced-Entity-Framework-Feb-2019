using System.Xml.Serialization;

namespace PetClinic.DataProcessor.Dto
{
    [XmlType("AnimalAid")]
    public class ImportAnimalAid
    {
        [XmlElement("Name")]
        public string Name { get; set; }
    }
}
