namespace Cinema.DataProcessor
{
    using Cinema.DataProcessor.ExportDto;
    using Data;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    public class Serializer
    {
        public static string ExportTopMovies(CinemaContext context, int rating)
        {
            var movies = context.Movies
                .Where(m => m.Rating >= rating * 1.0 && m.Projections.Any(p => p.Tickets.Any(t => t.Customer != null)))
                .Select(m => new
                {
                    MovieName = m.Title,
                    Rating = m.Rating.ToString("f2"),
                    TotalIncomes = m.Projections.Select(p => p.Tickets.Sum(t => t.Price)).Sum().ToString("f2"),
                    Customers = m.Projections
                       .SelectMany(p => p.Tickets.Select(t => t.Customer))
                       .Select(x => new
                       {
                           FirstName = x.FirstName,
                           LastName = x.LastName,
                           Balance = x.Balance.ToString("f2")
                       })
                       .OrderByDescending(c => c.Balance)
                       .ThenBy(c => c.FirstName)
                       .ThenBy(c => c.LastName)
                       .ToArray()
                })
                .OrderByDescending(m => double.Parse(m.Rating))
                .ThenByDescending(m => decimal.Parse(m.TotalIncomes))
                .Take(10)
                .ToArray();

            string result = JsonConvert.SerializeObject(movies, Newtonsoft.Json.Formatting.Indented);
            return result;
        }

        public static string ExportTopCustomers(CinemaContext context, int age)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportCustomerDto[]), new XmlRootAttribute("Customers"));

            var customers = context.Customers
                .Where(c => c.Age >= age)
                .Select(c => new ExportCustomerDto()
                {
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    SpentMoney = c.Tickets.Sum(t => t.Price).ToString("f2"),
                    SpentTime = TimeSpan.FromTicks((long)c.Tickets.Select(t => t.Projection).Select(p => p.Movie.Duration.Ticks).Sum()).ToString(@"hh\:mm\:ss")
                })
                .OrderByDescending(c => decimal.Parse(c.SpentMoney))
                .Take(10)
                .ToArray();


            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });
            serializer.Serialize(new StringWriter(builder), customers, namespaces);

            return builder.ToString();
        }
    }
}