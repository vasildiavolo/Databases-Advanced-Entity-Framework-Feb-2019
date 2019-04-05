﻿namespace VaporStore.DataProcessor.Dtos.Import
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ImportUserDto
    {
        [Required]
        [RegularExpression("^[A-Z][a-z]+ [A-Z][a-z]+$")]
        public string FullName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        [Range(3, 103)]
        public int Age { get; set; }

        public List<ImportCardDto> Cards { get; set; }

    }
}
