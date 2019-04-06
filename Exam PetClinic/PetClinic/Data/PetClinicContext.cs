namespace PetClinic.Data
{
    using Microsoft.EntityFrameworkCore;
    using PetClinic.Models;

    public class PetClinicContext : DbContext
    {
        public PetClinicContext() { }

        public PetClinicContext(DbContextOptions options)
            : base(options) { }

        public DbSet<Vet> Vets { get; set; }
        public DbSet<Passport> Passports { get; set; }
        public DbSet<Animal> Animals { get; set; }
        public DbSet<Procedure> Procedures { get; set; }
        public DbSet<AnimalAid> AnimalAids { get; set; }
        public DbSet<ProcedureAnimalAid> ProceduresAnimalAids { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Configuration.ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ProcedureAnimalAid>()
                .HasKey(pa => new { pa.AnimalAidId, pa.ProcedureId });

            builder.Entity<AnimalAid>()
                .HasAlternateKey(a => a.Name);

            builder.Entity<Vet>()
                .HasAlternateKey(v => v.PhoneNumber);
        }
    }
}
