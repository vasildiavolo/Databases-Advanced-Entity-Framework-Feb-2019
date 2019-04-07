using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Cinema.Data.Models
{
    [XmlType("Ticket")]
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [XmlElement("Price")]
        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Price { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        [XmlElement("ProjectionId")]
        [Required]
        public int ProjectionId { get; set; }
        public Projection Projection { get; set; }
    }
}
