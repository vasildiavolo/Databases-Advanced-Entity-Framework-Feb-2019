namespace VaporStore.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using Data;
    using Newtonsoft.Json;
    using VaporStore.Data.Models.Enumerators;
    using VaporStore.DataProcessor.Dtos.Export;

    public static class Serializer
    {
        public static string ExportGamesByGenres(VaporStoreDbContext context, string[] genreNames)
        {
            var genres = context.Genres
                .Where(gn => genreNames.Contains(gn.Name))
                .Select(gn => new
                {
                    Id = gn.Id,
                    Genre = gn.Name,

                    Games = gn.Games.Where(gm => gm.Purchases.Count > 0)
                        .OrderByDescending(gm => gm.Purchases.Count)
                        .ThenBy(gm => gm.Id)
                        .Select(gm => new
                        {
                            Id = gm.Id,
                            Title = gm.Name,
                            Developer = gm.Developer.Name,
                            Tags = string.Join(", ", gm.GameTags.Select(gt => gt.Tag.Name).ToArray()),
                            Players = gm.Purchases.Count
                        }),

                    TotalPlayers = gn.Games.Select(gm => gm.Purchases.Count).Sum()
                })
                .OrderByDescending(gn => gn.TotalPlayers)
                .ThenBy(gn => gn.Id)
                .ToArray();


            string result = JsonConvert.SerializeObject(genres, Newtonsoft.Json.Formatting.Indented);

            return result;
        }

        public static string ExportUserPurchasesByType(VaporStoreDbContext context, string storeType)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportUserDto[]), new XmlRootAttribute("Users"));

            var purchaseType = Enum.Parse<PurchaseType>(storeType);

            var users = context.Users
                .Select(x => new
                {
                    Username = x.Username,

                    Purchases = x.Cards
                    .SelectMany(y => y.Purchases)
                    .Where(h => h.Type == purchaseType)
                    .Select(z => new ExportPurchaseDto()
                    {
                        Card = z.Card.Number,
                        Cvc = z.Card.Cvc,
                        Date = z.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        Game = new ExportGameDto()
                        {
                            Title = z.Game.Name,
                            Genre = z.Game.Genre.Name,
                            Price = z.Game.Price
                        }
                    })
                    .OrderBy(l => DateTime.ParseExact(l.Date, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                    .ToList(),
                })
                .ToArray()
                .Select(f => new ExportUserDto()
                {
                    Username = f.Username,
                    Purchases = f.Purchases,
                    TotalSpent = f.Purchases.Sum(p => p.Game.Price)
                })
                .Where(x => x.Purchases.Any())
                .OrderByDescending(x => x.TotalSpent)
                .ThenBy(x => x.Username)
                .ToArray();


            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[]
            {
                new XmlQualifiedName("","")
            });

            serializer.Serialize(new StringWriter(builder), users, namespaces);

            var result = builder.ToString();
            
            return result;
        }
    }
}