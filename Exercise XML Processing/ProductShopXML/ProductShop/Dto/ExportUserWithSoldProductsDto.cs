using System.Xml.Serialization;

namespace ProductShop.Dto
{
    [XmlType("User")]
    public class ExportUserWithSoldProductsDto
    {
        [XmlElement("firstName")]
        public string FirstName { get; set; }

        [XmlElement("lastName")]
        public string LastName { get; set; }

        [XmlElement("age")]
        public int? Age { get; set; }

        [XmlElement("SoldProducts")]
        public ExportProductWithCountDto SoldProducts { get; set; }
    }
}
