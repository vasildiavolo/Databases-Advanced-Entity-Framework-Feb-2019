namespace Cinema.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Cinema.Data.Models;
    using Cinema.DataProcessor.ImportDto;
    using Data;
    using Newtonsoft.Json;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";
        private const string SuccessfulImportMovie
            = "Successfully imported {0} with genre {1} and rating {2:F2}!";
        private const string SuccessfulImportHallSeat
            = "Successfully imported {0}({1}) with {2} seats!";
        private const string SuccessfulImportProjection
            = "Successfully imported projection {0} on {1}!";
        private const string SuccessfulImportCustomerTicket
            = "Successfully imported customer {0} {1} with bought tickets: {2}!";

        public static string ImportMovies(CinemaContext context, string jsonString)
        {
            var importMovies = JsonConvert.DeserializeObject<Movie[]>(jsonString);
            var validMovies = new HashSet<Movie>();

            StringBuilder sb = new StringBuilder();

            foreach (var movie in importMovies)
            {
                if (!IsValid(movie) || validMovies.FirstOrDefault(m => m.Title == movie.Title) != null)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                validMovies.Add(movie);
                sb.AppendLine(string.Format(SuccessfulImportMovie, movie.Title, movie.Genre, movie.Rating));
            }

            context.Movies.AddRange(validMovies);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportHallSeats(CinemaContext context, string jsonString)
        {
            var importHalls = JsonConvert.DeserializeObject<ImportHallDto[]>(jsonString);
            var validHalls = new HashSet<Hall>();

            StringBuilder sb = new StringBuilder();

            foreach (var hallDto in importHalls)
            {
                if (!IsValid(hallDto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var hall = new Hall()
                {
                    Name = hallDto.Name,
                    Is4Dx = hallDto.Is4Dx,
                    Is3D = hallDto.Is3D
                };

                for (int seatNum = 0; seatNum < hallDto.Seats; seatNum++)
                {
                    hall.Seats.Add(new Seat());
                }

                validHalls.Add(hall);

                string projectionType = "";
                if (hall.Is3D && hall.Is4Dx)
                {
                    projectionType = "4Dx/3D";
                }
                else if (!hall.Is3D && !hall.Is4Dx)
                {
                    projectionType = "Normal";
                }
                else if (hall.Is3D)
                {
                    projectionType = "3D";
                }
                else if (hall.Is4Dx)
                {
                    projectionType = "4Dx";
                }

                sb.AppendLine(string.Format(SuccessfulImportHallSeat, hall.Name, projectionType, hall.Seats.Count));
            }

            context.Halls.AddRange(validHalls);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportProjections(CinemaContext context, string xmlString)
        {
            StringBuilder sb = new StringBuilder();
            XmlSerializer serializer = new XmlSerializer(typeof(ImportProjectionDto[]), new XmlRootAttribute("Projections"));
            var importProjections = (ImportProjectionDto[])serializer.Deserialize(new StringReader(xmlString));
            var validProjections = new List<Projection>();

            var moviesIds = context.Movies.Select(m => m.Id).ToArray();
            var hallsIds = context.Halls.Select(h => h.Id).ToArray();

            foreach (var dto in importProjections)
            {
                if (!IsValid(dto) ||
                    !moviesIds.Contains(dto.MovieId) ||
                    !hallsIds.Contains(dto.HallId))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var projection = new Projection()
                {
                    MovieId = dto.MovieId,
                    HallId = dto.HallId,
                    DateTime = DateTime.ParseExact(dto.DateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                };

                validProjections.Add(projection);

                string movieTitle = context.Movies.Find(projection.MovieId).Title;
                string projectionDateTime = projection.DateTime.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                sb.AppendLine(string.Format(SuccessfulImportProjection, movieTitle, projectionDateTime));
            }

            context.Projections.AddRange(validProjections);
            context.SaveChanges();

            string result = sb.ToString().Trim();
            return result;
        }

        public static string ImportCustomerTickets(CinemaContext context, string xmlString)
        {
            StringBuilder sb = new StringBuilder();
            XmlSerializer serializer = new XmlSerializer(typeof(ImportCustomerDto[]), new XmlRootAttribute("Customers"));
            var importCustomers = (ImportCustomerDto[])serializer.Deserialize(new StringReader(xmlString));
            var validCustomers = new List<Customer>();

            foreach (var dto in importCustomers)
            {
                bool isValidTickets = true;

                if (!IsValid(dto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var customer = new Customer()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Age = dto.Age,
                    Balance = dto.Balance
                };

                foreach (var importTicket in dto.Tickets)
                {
                    if (!IsValid(importTicket))
                    {
                        sb.AppendLine(ErrorMessage);
                        isValidTickets = false;
                        break;
                    }

                    var ticket = new Ticket()
                    {
                        Price = importTicket.Price,
                        ProjectionId = importTicket.ProjectionId,
                        Customer = customer
                    };

                    customer.Tickets.Add(ticket);
                }

                if (isValidTickets)
                {
                    validCustomers.Add(customer);
                    sb.AppendLine(string.Format(SuccessfulImportCustomerTicket, customer.FirstName, customer.LastName, customer.Tickets.Count));
                }
            }

            int tickets = validCustomers.Sum(c => c.Tickets.Count);
            
            context.Customers.AddRange(validCustomers);
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