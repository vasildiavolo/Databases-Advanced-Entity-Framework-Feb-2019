using System.ComponentModel.DataAnnotations;

namespace PetClinic.DataProcessor.Dto
{
    public class ImportAnimalDto
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Name { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Type { get; set; }

        [Required]
        [Range(1, 2147483647)]
        public int Age { get; set; }

        [Required]
        public ImportPassportDto Passport { get; set; }
    }
}
