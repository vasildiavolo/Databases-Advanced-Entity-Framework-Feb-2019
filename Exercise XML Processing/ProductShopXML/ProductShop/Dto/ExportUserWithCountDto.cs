namespace ProductShop.Dto
{
    using System.Xml.Serialization;

    [XmlType("Users")]
    public class ExportUserWithCountDto
    {
        [XmlElement("count")]
        public int Count { get; set; }

        [XmlArray("users")]
        public ExportUserWithSoldProductsDto[] Users { get; set; }
    }
}
