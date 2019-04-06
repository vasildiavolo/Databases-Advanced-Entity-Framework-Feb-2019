using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace PetClinic.DataProcessor.Dto
{
    [XmlType("Procedure")]
    public class ImportProcedureDto
    {
        [Required]
        [XmlElement("Vet")]
        public string Vet { get; set; }

        [Required]
        [XmlElement("Animal")]
        public string Animal { get; set; }

        [Required]
        [XmlElement("DateTime")]
        public string DateTime { get; set; }

        [XmlArray("AnimalAids")]
        public ImportAnimalAid[] AnimalAids { get; set; }
    }
}
