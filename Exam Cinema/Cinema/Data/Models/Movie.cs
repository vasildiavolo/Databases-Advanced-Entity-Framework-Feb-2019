using Cinema.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cinema.Data.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20, MinimumLength =3)]
        public string Title { get; set; }

        [Required]
        public Genre Genre { get; set; }

        [Required]
        public TimeSpan Duration { get; set; }

        [Required]
        [Range(1.0, 10.0)]
        public double Rating { get; set; }

        [Required]
        [StringLength(20, MinimumLength =3)]
        public string Director { get; set; }

        public ICollection<Projection> Projections { get; set; } = new HashSet<Projection>();
    }
}
