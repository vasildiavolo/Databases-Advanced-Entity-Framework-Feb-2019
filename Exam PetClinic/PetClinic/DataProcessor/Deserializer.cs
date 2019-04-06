namespace PetClinic.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using PetClinic.Data;
    using PetClinic.DataProcessor.Dto;
    using PetClinic.Models;

    public class Deserializer
    {
        private const string errorMessage = "Error: Invalid data.";

        public static string ImportAnimalAids(PetClinicContext context, string jsonString)
        {
            var importAnimalAids = JsonConvert.DeserializeObject<AnimalAid[]>(jsonString);
            var validAnimalAids = new HashSet<AnimalAid>();
            StringBuilder sb = new StringBuilder();

            foreach (var animalAid in importAnimalAids)
            {
                if (!IsValid(animalAid) || validAnimalAids.FirstOrDefault(aa => aa.Name == animalAid.Name) != null)
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                validAnimalAids.Add(animalAid);
                sb.AppendLine($"Record {animalAid.Name} successfully imported.");
            }

            context.AnimalAids.AddRange(validAnimalAids);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportAnimals(PetClinicContext context, string jsonString)
        {
            var importAnimals = JsonConvert.DeserializeObject<ImportAnimalDto[]>(jsonString);
            var validAnimals = new HashSet<Animal>();
            StringBuilder sb = new StringBuilder();

            foreach (var animalDto in importAnimals)
            {
                if (!IsValid(animalDto) ||
                    !IsValid(animalDto.Passport) ||
                    validAnimals.FirstOrDefault(a => a.PassportSerialNumber == animalDto.Passport.SerialNumber) != null)
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                var animal = new Animal()
                {
                    Name = animalDto.Name,
                    Type = animalDto.Type,
                    Age = animalDto.Age

                };

                var passport = new Passport()
                {
                    SerialNumber = animalDto.Passport.SerialNumber,
                    OwnerName = animalDto.Passport.OwnerName,
                    OwnerPhoneNumber = animalDto.Passport.OwnerPhoneNumber,
                    RegistrationDate = DateTime.ParseExact(animalDto.Passport.RegistrationDate, "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    Animal = animal
                };

                animal.PassportSerialNumber = passport.SerialNumber;
                animal.Passport = passport;

                validAnimals.Add(animal);
                sb.AppendLine($"Record {animal.Name} Passport №: {animal.Passport.SerialNumber} successfully imported.");
            }

            context.Animals.AddRange(validAnimals);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportVets(PetClinicContext context, string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportVetDto[]), new XmlRootAttribute("Vets"));
            var importVets = (ImportVetDto[])serializer.Deserialize(new StringReader(xmlString));
            var vets = new HashSet<Vet>();
            StringBuilder sb = new StringBuilder();

            foreach (var vetDto in importVets)
            {
                if (!IsValid(vetDto) ||
                    vets.FirstOrDefault(v => v.PhoneNumber == vetDto.PhoneNumber) != null)
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                vets.Add(new Vet()
                {
                    Name = vetDto.Name,
                    Age = vetDto.Age,
                    PhoneNumber = vetDto.PhoneNumber,
                    Profession = vetDto.Profession
                });

                sb.AppendLine($"Record {vetDto.Name} successfully imported.");
            }

            context.Vets.AddRange(vets);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportProcedures(PetClinicContext context, string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportProcedureDto[]), new XmlRootAttribute("Procedures"));
            var importProcedures = (ImportProcedureDto[])serializer.Deserialize(new StringReader(xmlString));
            var procedures = new HashSet<Procedure>();
            StringBuilder sb = new StringBuilder();
            
            foreach (var procedureDto in importProcedures)
            {
                bool valid = true;

                var dtoVet = context.Vets.FirstOrDefault(v => v.Name == procedureDto.Vet);
                var dtoAnimal = context.Animals.FirstOrDefault(a => a.PassportSerialNumber == procedureDto.Animal);

                if (!IsValid(procedureDto) || dtoVet == null || dtoAnimal == null)
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                var procedure = new Procedure()
                {
                    Vet = dtoVet,
                    Animal = dtoAnimal,
                    DateTime = DateTime.ParseExact(procedureDto.DateTime, "dd-MM-yyyy", CultureInfo.InvariantCulture)
                };

                var animalAidNames = context.AnimalAids.Select(aa => aa.Name).ToArray();

                foreach (var animalAidDto in procedureDto.AnimalAids)
                {
                    if (!animalAidNames.Contains(animalAidDto.Name))
                    {
                        sb.AppendLine(errorMessage);
                        valid = false;
                        break;
                    }

                    if (procedure.ProcedureAnimalAids.Select(pa => pa.AnimalAid.Name).Contains(animalAidDto.Name))
                    {
                        sb.AppendLine(errorMessage);
                        valid = false;
                        break;
                    }

                    procedure.ProcedureAnimalAids.Add(new ProcedureAnimalAid()
                    {
                        AnimalAid = context.AnimalAids.FirstOrDefault(aa => aa.Name == animalAidDto.Name)
                    });
                }

                if (valid)
                {
                    procedures.Add(procedure);
                    sb.AppendLine("Record successfully imported.");
                }
            }
            
            context.Procedures.AddRange(procedures);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        private static bool IsValid(object entity)
        {
            var validationContext = new ValidationContext(entity);
            var validationResults = new List<ValidationResult>();

            return Validator.TryValidateObject(entity, validationContext, validationResults, true);
        }

    }
}
