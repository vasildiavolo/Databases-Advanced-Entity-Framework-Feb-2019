namespace SoftJail.DataProcessor
{
    using Data;
    using Newtonsoft.Json;
    using SoftJail.Data.Models;
    using SoftJail.Data.Models.Enum;
    using SoftJail.DataProcessor.ImportDto;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    public class Deserializer
    {
        private const string errorMessage = "Invalid Data";

        public static string ImportDepartmentsCells(SoftJailDbContext context, string jsonString)
        {
            StringBuilder sb = new StringBuilder();

            var departmentsDto = JsonConvert.DeserializeObject<ImportDepartmentDto[]>(jsonString);

            var departments = new List<Department>();

            foreach (var departmentDto in departmentsDto)
            {
                bool valid = true;

                if (!IsValid(departmentDto))
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                var deparment = new Department()
                {
                    Name = departmentDto.Name,
                };

                foreach (var cellDto in departmentDto.Cells)
                {
                    if (!IsValid(cellDto))
                    {
                        sb.AppendLine(errorMessage);
                        valid = false;
                        break;
                    }

                    var cell = new Cell()
                    {
                        CellNumber = cellDto.CellNumber,
                        HasWindow = cellDto.HasWindow
                    };

                    deparment.Cells.Add(cell);
                }
                ;
                if (valid)
                {
                    departments.Add(deparment);
                    sb.AppendLine($"Imported {deparment.Name} with {deparment.Cells.Count} cells");
                }
            }
            ;
            context.Departments.AddRange(departments);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportPrisonersMails(SoftJailDbContext context, string jsonString)
        {
            StringBuilder sb = new StringBuilder();

            var prisonersDto = JsonConvert.DeserializeObject<ImportPrisonerDto[]>(jsonString);
            var prisoners = new List<Prisoner>();

            foreach (var prDto in prisonersDto)
            {
                bool valid = true;

                if (!IsValid(prDto))
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                var pr = new Prisoner()
                {
                    FullName = prDto.FullName,
                    Nickname = prDto.Nickname,
                    Age = prDto.Age,
                    IncarcerationDate = DateTime.ParseExact(prDto.IncarcerationDate, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    CellId = prDto.CellId,
                    Bail = prDto.Bail
                };

                if (prDto.ReleaseDate != null)
                {
                    pr.ReleaseDate = DateTime.ParseExact(prDto.ReleaseDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }

                foreach (var mDto in prDto.Mails)
                {
                    if (!IsValid(mDto))
                    {
                        sb.AppendLine(errorMessage);
                        valid = false;
                        break;
                    }

                    var mail = new Mail()
                    {
                        Description = mDto.Description,
                        Address = mDto.Address,
                        Sender = mDto.Sender
                    };

                    pr.Mails.Add(mail);
                }

                if (valid)
                {
                    prisoners.Add(pr);
                    sb.AppendLine($"Imported {pr.FullName} {pr.Age} years old");
                }
            }

            context.Prisoners.AddRange(prisoners);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportOfficersPrisoners(SoftJailDbContext context, string xmlString)
        {
            StringBuilder sb = new StringBuilder();
            XmlSerializer serializer = new XmlSerializer(typeof(ImportOfficerDto[]), new XmlRootAttribute("Officers"));
            var officersDto = (ImportOfficerDto[])serializer.Deserialize(new StringReader(xmlString));
            var officers = new List<Officer>();

            foreach (var oDto in officersDto)
            {
                if (!IsValid(oDto) ||
                    !Enum.TryParse(oDto.Weapon, out Weapon weaponEnum) ||
                    !Enum.TryParse(oDto.Position, out Position positionEnum))
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                var officer = new Officer()
                {
                    FullName = oDto.FullName,
                    Salary = oDto.Salary,
                    DepartmentId = oDto.DepartmentId,
                    Weapon = Enum.Parse<Weapon>(oDto.Weapon),
                    Position = Enum.Parse<Position>(oDto.Position),
                    OfficerPrisoners = oDto
                        .Prisoners
                        .Select(x => new OfficerPrisoner()
                        {
                            PrisonerId = x.Id
                        })
                        .ToArray()
                };

                officers.Add(officer);
                sb.AppendLine($"Imported {officer.FullName} ({officer.OfficerPrisoners.Count} prisoners)");

            }

            int count = officers.SelectMany(o => o.OfficerPrisoners).Count();

            context.Officers.AddRange(officers);
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