using System.ComponentModel.DataAnnotations;

namespace Cinema.DataProcessor.ImportDto
{
    public class ImportHallDto
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Name { get; set; }

        [Required]
        public bool Is4Dx { get; set; }

        [Required]
        public bool Is3D { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Seats { get; set; }
    }
}
