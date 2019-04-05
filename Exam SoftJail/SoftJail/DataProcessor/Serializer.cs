namespace SoftJail.DataProcessor
{
    using Data;
    using Newtonsoft.Json;
    using SoftJail.DataProcessor.ExportDto;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    public class Serializer
    {
        public static string ExportPrisonersByCells(SoftJailDbContext context, int[] ids)
        {
            var prisoners = context.Prisoners
                .Where(p => ids.Contains(p.Id))
                .Select(p => new
                {
                    Id = p.Id,
                    Name = p.FullName,
                    CellNumber = p.Cell.CellNumber,

                    Officers = p.PrisonerOfficers
                        .Select(po => new
                        {
                            OfficerName = po.Officer.FullName,
                            Department = po.Officer.Department.Name
                        })
                        .OrderBy(o => o.OfficerName)
                        .ToArray(),

                    TotalOfficerSalary = p.PrisonerOfficers
                        .Sum(po => po.Officer.Salary)
                })
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ToArray();

            var result = JsonConvert.SerializeObject(prisoners, Newtonsoft.Json.Formatting.Indented);
            return result;
        }

        public static string ExportPrisonersInbox(SoftJailDbContext context, string prisonersNames)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportPrisonerDto[]), new XmlRootAttribute("Prisoners"));
            var validPrisonersNames = prisonersNames.Split(",", StringSplitOptions.RemoveEmptyEntries);
            
            var prisoners = context.Prisoners
                .Where(p => validPrisonersNames.Contains(p.FullName))
                .Select(p => new ExportPrisonerDto()
                {
                    Id = p.Id,
                    Name = p.FullName,
                    IncarcerationDate = p.IncarcerationDate.ToString("yyyy-MM-dd"),
                    EncryptedMessages = p.Mails
                        .Select(m => new ExportMailDto()
                        {
                            Description = ReverseString(m.Description)
                        })
                        .ToArray()
                })
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ToArray();
            
            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });
            serializer.Serialize(new StringWriter(builder), prisoners, namespaces);

            return builder.ToString();
        }

        private static string ReverseString(string inputString)
        {
            StringBuilder resultBuilder = new StringBuilder();

            for (int i = inputString.Length - 1; i >= 0 ; i--)
            {
                resultBuilder.Append(inputString[i]);
            }

            string resultString = resultBuilder.ToString().Trim();

            return resultString;
        }
    }
}