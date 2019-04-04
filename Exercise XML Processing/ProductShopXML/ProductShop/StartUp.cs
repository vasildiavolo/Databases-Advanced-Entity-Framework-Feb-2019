namespace ProductShop
{
    using AutoMapper;
    using ProductShop.Data;
    using ProductShop.Dto;
    using ProductShop.Dto.Import;
    using ProductShop.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    public class StartUp
    {
        //private static readonly string path = @"C:\Users\Vasil Evlogiev\source\repos\ProductShop\Datasets\categories-products.xml";
        //private static readonly string xml = File.ReadAllText(path);

        public static void Main(string[] args)
        {
            Mapper.Initialize(m =>
            {
                m.AddProfile<ProductShopProfile>();
            });

            using (var context = new ProductShopContext())
            {
                string result = GetUsersWithProducts(context);
                Console.WriteLine(result);
            }
        }

        public static string GetUsersWithProducts(ProductShopContext context)
        {
            //Select all users who have at least 1 sold product. Take only first 10.
            //Order them by the number of sold products (from highest to lowest). 
            //Select only their first and last name, age, count of sold products and for each product - name and price sorted by price (descending).

            XmlSerializer serializer = new XmlSerializer(typeof(ExportUserWithCountDto), new XmlRootAttribute("Users"));

            var insideUsers = context.Users
                .Where(u => u.ProductsSold.Any(ps => ps.Buyer != null))
                .OrderByDescending(u => u.ProductsSold.Count)
                .Select(u => new ExportUserWithSoldProductsDto
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Age = u.Age,
                    SoldProducts = new ExportProductWithCountDto()
                    {
                        Count = u.ProductsSold.Count,
                        Products = u.ProductsSold.Select(ps => new ExportProductDto()
                        {
                            Name = ps.Name,
                            Price = ps.Price
                        })
                        .OrderByDescending(epdto => epdto.Price)
                        .ToArray()
                    }
                })
                .Take(10)
                .ToArray();

            var users = new ExportUserWithCountDto()
            {
                Count = context.Users
                .Where(u => u.ProductsSold.Any(ps => ps.Buyer != null)).Count(),
                Users = insideUsers
            };

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[]
            {
                new XmlQualifiedName("","")
            });

            serializer.Serialize(new StringWriter(builder), users, namespaces);

            return builder.ToString();
        }

        public static string GetCategoriesByProductsCount(ProductShopContext context)
        {
            //Get all categories. 
            //For each category select its name, the number of products, the average price of those products and the total revenue (total price sum) of those products (regardless if they have a buyer or not). 
            //Order them by the number of products (descending) then by total revenue.

            XmlSerializer serializer = new XmlSerializer(typeof(ExportCategoryDto[]), new XmlRootAttribute("Categories"));

            var categories = context.Categories
                .Select(c => new ExportCategoryDto
                {
                    Name = c.Name,
                    Count = c.CategoryProducts.Count,
                    AveragePrice = c.CategoryProducts.Average(x => x.Product.Price),
                    TotalRevenue = c.CategoryProducts.Sum(x => x.Product.Price)
                })
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.TotalRevenue)
                .ToArray(); ;

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[]
            {
                new XmlQualifiedName("","")
            });

            serializer.Serialize(new StringWriter(builder), categories, namespaces);

            return builder.ToString();
        }

        public static string GetSoldProducts(ProductShopContext context)
        {
            //Get all users who have at least 1 sold item. Order them by last name, then by first name. Select the person's first and last name. For each of the sold products, select the product's name and price. Take top 5 records. 

            XmlSerializer serializer = new XmlSerializer(typeof(ExportUserDto[]), new XmlRootAttribute("Users"));

            var users = context.Users
                .Where(u => u.ProductsSold.Any(sp => sp.Buyer != null))
                .Select(u => new ExportUserDto
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    SoldProducts = u.ProductsSold.Select(ps => new ExportProductDto
                    {
                        Name = ps.Name,
                        Price = ps.Price
                    })
                    .ToArray()
                })
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(5)
                .ToArray();

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[]
            {
                new XmlQualifiedName("","")
            });

            serializer.Serialize(new StringWriter(builder), users, namespaces);

            return builder.ToString();
        }

        public static string GetProductsInRange(ProductShopContext context)
        {
            //Get all products in a specified price range between 500 and 1000 (inclusive). Order them by price (from lowest to highest). Select only the product name, price and the full name of the buyer. Take top 10 records.

            XmlSerializer serializer = new XmlSerializer(typeof(ExportProductDto[]), new XmlRootAttribute("Products"));

            var products = context.Products
                .Where(p => p.Price >= 500 && p.Price <= 1000)
                .Select(p => new ExportProductDto
                {
                    Name = p.Name,
                    Price = p.Price,
                    BuyerFullName = p.Buyer.FirstName + " " + p.Buyer.LastName
                })
                .OrderBy(p => p.Price)
                .Take(10)
                .ToArray();

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[]
            {
                new XmlQualifiedName("","")
            });

            serializer.Serialize(new StringWriter(builder), products, namespaces);

            return builder.ToString();
        }

        public static string ImportCategoryProducts(ProductShopContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CategoryProductDto[]), new XmlRootAttribute("CategoryProducts"));

            var categoryProductsDto = (CategoryProductDto[])serializer.Deserialize(new StringReader(inputXml));

            var categoryProducts = new List<CategoryProduct>();

            var categoriesId = context.Categories.Select(c => c.Id).ToArray();
            var productsId = context.Products.Select(p => p.Id).ToArray();

            foreach (var categoryDto in categoryProductsDto)
            {
                if (categoriesId.Contains(categoryDto.CategoryId) && productsId.Contains(categoryDto.ProductId))
                {
                    var categoryProduct = Mapper.Map<CategoryProduct>(categoryDto);
                    categoryProducts.Add(categoryProduct);
                }
            }

            context.CategoryProducts.AddRange(categoryProducts);
            context.SaveChanges();

            return $"Successfully imported {categoryProducts.Count}";

        }

        public static string ImportCategories(ProductShopContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CategoryDto[]), new XmlRootAttribute("Categories"));

            var categoriesDto = (CategoryDto[])serializer.Deserialize(new StringReader(inputXml));

            var categories = new List<Category>();

            foreach (var categoryDto in categoriesDto)
            {
                var category = Mapper.Map<Category>(categoryDto);
                categories.Add(category);
            }

            context.Categories.AddRange(categories);
            context.SaveChanges();

            return $"Successfully imported {categories.Count}";
        }

        public static string ImportProducts(ProductShopContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ProductDto[]), new XmlRootAttribute("Products"));

            var productsDto = (ProductDto[])serializer.Deserialize(new StringReader(inputXml));

            var products = new List<Product>();

            var usersId = context.Users.Select(u => u.Id).ToArray();

            foreach (var productDto in productsDto)
            {
                if (usersId.Contains(productDto.BuyerId) && usersId.Contains(productDto.SellerId))
                {
                    var product = Mapper.Map<Product>(productDto);
                    products.Add(product);
                }
            }

            context.Products.AddRange(products);
            context.SaveChanges();

            return $"Successfully imported {products.Count}";
        }

        public static string ImportUsers(ProductShopContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(UserDto[]), new XmlRootAttribute("Users"));

            var usersDto = (UserDto[])serializer.Deserialize(new StringReader(inputXml));

            var users = new List<User>();

            foreach (var userDto in usersDto)
            {
                var user = Mapper.Map<User>(userDto);
                users.Add(user);
            }

            context.Users.AddRange(users);
            context.SaveChanges();

            return $"Successfully imported {users.Count}";
        }
    }
}
