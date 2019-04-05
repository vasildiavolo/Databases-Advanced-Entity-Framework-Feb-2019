using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SoftJail.DataProcessor.ImportDto
{
    public class ImportDepartmentDto
    {
        [Required]
        [StringLength(25, MinimumLength = 3)]
        public string Name { get; set; }

        public ICollection<ImportCellDto> Cells { get; set; } = new List<ImportCellDto>();

    }
}
