namespace PetClinic.DataProcessor
{
    using Newtonsoft.Json;
    using PetClinic.Data;
    using PetClinic.DataProcessor.Dto;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    public class Serializer
    {
        public static string ExportAnimalsByOwnerPhoneNumber(PetClinicContext context, string phoneNumber)
        {
            var animals = context.Animals
                .Where(a => a.Passport.OwnerPhoneNumber == phoneNumber)
                .Select(a => new
                {
                    OwnerName = a.Passport.OwnerName,
                    AnimalName = a.Name,
                    Age = a.Age,
                    SerialNumber = a.Passport.SerialNumber,
                    RegisteredOn = a.Passport.RegistrationDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                })
                .OrderBy(a => a.Age)
                .ThenBy(a => a.SerialNumber)
                .ToArray();

            string result = JsonConvert.SerializeObject(animals, Newtonsoft.Json.Formatting.Indented);
            return result;
        }

        public static string ExportAllProcedures(PetClinicContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportProcedureDto[]), new XmlRootAttribute("Procedures"));

            var procedures = context.Procedures
                .Select(p => new ExportProcedureDto()
                {
                    Passport = p.Animal.PassportSerialNumber,
                    OwnerNumber = p.Animal.Passport.OwnerPhoneNumber,
                    DateTime = p.DateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),

                    AnimalAids = p.ProcedureAnimalAids
                        .Select(pa => pa.AnimalAid)
                        .Select(aa => new ExportAnimalAid()
                        {
                            Name = aa.Name,
                            Price = aa.Price
                        })
                        .ToList(),

                    TotalPrice = p.ProcedureAnimalAids
                        .Sum(pa => pa.AnimalAid.Price)
                })
                .OrderBy(p => DateTime.ParseExact(p.DateTime, "dd-MM-yyyy", CultureInfo.InvariantCulture))
                .ThenBy(p => p.Passport)
                .ToArray();            

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });
            serializer.Serialize(new StringWriter(builder), procedures, namespaces);

            return builder.ToString();
        }
    }
}
