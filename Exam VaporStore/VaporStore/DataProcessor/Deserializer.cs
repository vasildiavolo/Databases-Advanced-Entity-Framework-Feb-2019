namespace VaporStore.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Data;
    using Newtonsoft.Json;
    using VaporStore.Data.Models;
    using VaporStore.Data.Models.Enumerators;
    using VaporStore.DataProcessor.Dtos.Import;

    public static class Deserializer
    {
        private static readonly string errorMessage = "Invalid Data";

        public static string ImportGames(VaporStoreDbContext context, string jsonString)
        {
            var gamesDto = JsonConvert.DeserializeObject<ImportGameDto[]>(jsonString);
            var games = new List<Game>();

            StringBuilder sb = new StringBuilder();

            foreach (var gameDto in gamesDto)
            {
                if (!IsValid(gameDto) || gameDto.Tags.Length == 0)
                {
                    sb.AppendLine("Invalid Data");
                    continue;
                }

                var game = new Game
                {
                    Name = gameDto.Name,
                    Price = gameDto.Price,
                    ReleaseDate = DateTime.ParseExact(gameDto.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                };

                game.Developer = GetDeveloper(context, gameDto.Developer);
                game.Genre = GetGenre(context, gameDto.Genre);

                foreach (var tag in gameDto.Tags)
                {
                    var currentTag = (Tag)GetTag(context, tag);

                    game.GameTags.Add(new GameTag
                    {
                        Tag = currentTag,
                        Game = game
                    });
                }

                games.Add(game);
                sb.AppendLine($"Added {game.Name} ({game.Genre.Name}) with {game.GameTags.Count} tags");
            }
            ;

            context.Games.AddRange(games);
            context.SaveChanges();

            string result = sb.ToString().Trim();

            return result;
        }

        private static object GetTag(VaporStoreDbContext context, string tag)
        {
            var currentTag = context.Tags.FirstOrDefault(d => d.Name == tag);

            if (currentTag == null)
            {
                currentTag = new Tag() { Name = tag };

                context.Tags.Add(currentTag);
                context.SaveChanges();
            }

            return currentTag;
        }

        private static Genre GetGenre(VaporStoreDbContext context, string genre)
        {
            var currentGenre = context.Genres.FirstOrDefault(d => d.Name == genre);

            if (currentGenre == null)
            {
                currentGenre = new Genre() { Name = genre };

                context.Genres.Add(currentGenre);
                context.SaveChanges();
            }

            return currentGenre;
        }

        private static Developer GetDeveloper(VaporStoreDbContext context, string developer)
        {
            var currentDeveloper = context.Developers.FirstOrDefault(d => d.Name == developer);

            if (currentDeveloper == null)
            {
                currentDeveloper = new Developer() { Name = developer };

                context.Developers.Add(currentDeveloper);
                context.SaveChanges();
            }

            return currentDeveloper;
        }
        
        public static string ImportUsers(VaporStoreDbContext context, string jsonString)
        {
            var usersDto = JsonConvert.DeserializeObject<ImportUserDto[]>(jsonString);
            var users = new List<User>();

            StringBuilder sb = new StringBuilder();
            
            foreach (var userDto in usersDto)
            {
                bool checkForAllValidCards = true;

                if (!IsValid(userDto) || userDto.Cards.Count == 0)
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                var user = new User()
                {
                    FullName = userDto.FullName,
                    Username = userDto.Username,
                    Email = userDto.Email,
                    Age = userDto.Age
                };
                ;
                foreach (var card in userDto.Cards)
                {
                    if (!IsValid(card) || (card.Type != "Debit" && card.Type != "Credit"))
                    {
                        checkForAllValidCards = false;
                        break;
                    }

                    user.Cards.Add(new Card()
                    {
                        Number = card.Number,
                        Cvc = card.CVC,
                        Type = card.Type == "Debit" ? CardType.Debit : CardType.Credit
                    });
                }

                if (checkForAllValidCards == false)
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                users.Add(user);
                sb.AppendLine($"Imported {user.Username} with {user.Cards.Count} cards");
            }
            ;
            context.Users.AddRange(users);
            context.SaveChanges();

            string result = sb.ToString().Trim();

            return result;
        }

        public static string ImportPurchases(VaporStoreDbContext context, string xmlString)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportPurchaseDto[]), new XmlRootAttribute("Purchases"));

            var purchasesDto = (ImportPurchaseDto[])serializer.Deserialize(new StringReader(xmlString));
            var purchases = new List<Purchase>();

            StringBuilder sb = new StringBuilder();
            
            foreach (var purchaseDto in purchasesDto)
            {
                if (!IsValid(purchasesDto) || (purchaseDto.Type != "Retail" && purchaseDto.Type != "Digital"))
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }
                
                var purchase = new Purchase
                {
                    Type = purchaseDto.Type == "Retail" ? PurchaseType.Retail : PurchaseType.Digital,
                    ProductKey = purchaseDto.Key,
                    Date = DateTime.ParseExact(purchaseDto.Date, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                };

                purchase.Card = context.Cards.FirstOrDefault(c => c.Number == purchaseDto.Card);
                purchase.Game = context.Games.FirstOrDefault(g => g.Name == purchaseDto.Title);

                if (purchase.Card == null || purchase.Game == null)
                {
                    sb.AppendLine(errorMessage);
                    continue;
                }

                purchases.Add(purchase);

                sb.AppendLine($"Imported {purchase.Game.Name} for {purchase.Card.User.Username}");
            }
            
            context.Purchases.AddRange(purchases);
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