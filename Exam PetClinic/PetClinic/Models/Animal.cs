using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinic.Models
{
    public class Animal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20, MinimumLength =3)]
        public string Name { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Type { get; set; }

        [Required]
        [Range(1, 2147483647)]
        public int Age { get; set; }

        [Required]
        [ForeignKey("Passport")]
        [RegularExpression("^[a-zA-Z]{7}[0-9]{3}$")]
        public string PassportSerialNumber { get; set; }
        public Passport Passport { get; set; }

        public ICollection<Procedure> Procedures { get; set; } = new HashSet<Procedure>();
    }
}
