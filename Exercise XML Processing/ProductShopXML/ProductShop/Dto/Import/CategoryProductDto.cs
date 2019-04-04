﻿namespace ProductShop.Dto.Import
{
    using System.Xml.Serialization;

    [XmlType("CategoryProduct")]
    public class CategoryProductDto
    {
        [XmlElement("CategoryId")]
        public int CategoryId { get; set; }

        [XmlElement("ProductId")]
        public int ProductId { get; set; }
    }
}
