namespace ProductShop.Dto
{
    using System.Xml.Serialization;

    [XmlType("SoldProducts")]
    public class ExportProductWithCountDto
    {
        [XmlElement("count")]
        public int Count { get; set; }

        [XmlArray("products")]
        public ExportProductDto[] Products { get; set; }
    }
}
