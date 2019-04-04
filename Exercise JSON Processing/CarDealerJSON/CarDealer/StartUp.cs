namespace CarDealer
{
    using CarDealer.Data;
    using CarDealer.Dto;
    using CarDealer.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public class StartUp
    {
        public static void Main(string[] args)
        {
            //string path = @"C:\Users\Vasil Evlogiev\source\repos\CarDealer\Datasets\sales.json";
            //string json = File.ReadAllText(path);

            using (var context = new CarDealerContext())
            {
                var result = GetSalesWithAppliedDiscount(context);
                Console.WriteLine(result);
            }
        }

        public static string GetSalesWithAppliedDiscount(CarDealerContext context)
        {
            //Get first 10 sales with information about the car, customer and price of the sale with and without discount.

            var sales = context.Sales
                .Select(s => new
                {
                    car = new
                    {
                        s.Car.Make,
                        s.Car.Model,
                        s.Car.TravelledDistance
                    },
                    customerName = s.Customer.Name,
                    s.Discount,
                    price = s.Car.PartCars.Select(pc => pc.Part.Price).Sum()
                })
                .Take(10)
                .ToArray()
                .Select(x => new
                {
                    x.car,
                    x.customerName,
                    Discount = x.Discount.ToString("f2"),
                    price = x.price.ToString("f2"),
                    priceWithDiscount = (x.price - ((x.Discount / 100.0m) * x.price)).ToString("f2")
                })
                .ToArray();

            return JsonConvert.SerializeObject(sales, Formatting.Indented);
        }

        public static string GetTotalSalesByCustomer(CarDealerContext context)
        {
            //Get all customers that have bought at least 1 car and get their names, bought cars count and total spent money on cars. 
            //Order the result list by total spent money descending then by total bought cars again in descending order. 

            var customers = context.Customers
                .Where(c => c.Sales.Count > 0)
                .Select(c => new
                {
                    fullName = c.Name,
                    boughtCars = c.Sales.Count,
                    sales = c.Sales.Select(s => new
                    {
                        Price = s.Car.PartCars.Select(pc => pc.Part.Price).Sum(),
                        s.Discount
                    })
                    .ToArray()
                })
                .ToArray()
                .Select(x => new
                {
                    x.fullName,
                    x.boughtCars,
                    spentMoney = x.sales.Sum(s => s.Price)
                })
                .OrderByDescending(c => c.spentMoney)
                .ThenByDescending(c => c.boughtCars)
                .ToArray();

            return JsonConvert.SerializeObject(customers, Formatting.Indented);
        }

        public static string GetCarsWithTheirListOfParts(CarDealerContext context)
        {
            //Get all cars along with their list of parts. 
            //For the car get only make, model and travelled distance and for the parts get only name and price (formatted to 2nd digit after the decimal point). 

            var cars = context.Cars
                .Select(c => new
                {
                    car = new
                    {
                        c.Make,
                        c.Model,
                        c.TravelledDistance
                    },

                    parts = c.PartCars.Select(pc => new
                    {
                        pc.Part.Name,
                        Price = pc.Part.Price.ToString("f2")
                    })
                })
                .ToArray();

            return JsonConvert.SerializeObject(cars, Formatting.Indented);
        }

        public static string GetLocalSuppliers(CarDealerContext context)
        {
            //Get all suppliers that do not import parts from abroad. Get their id, name and the number of parts they can offer to supply. 

            var suppliers = context.Suppliers
                .Where(s => s.IsImporter == false)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    PartsCount = s.Parts.Count
                })
                .ToArray();

            return JsonConvert.SerializeObject(suppliers, Formatting.Indented);
        }

        public static string GetCarsFromMakeToyota(CarDealerContext context)
        {
            var cars = context.Cars
                .Where(c => c.Make == "Toyota")
                .Select(c => new
                {
                    c.Id,
                    c.Make,
                    c.Model,
                    c.TravelledDistance
                })
                .OrderBy(c => c.Model)
                .ThenByDescending(c => c.TravelledDistance)
                .ToArray();

            var result = JsonConvert.SerializeObject(cars, Formatting.Indented);

            return result;
        }

        public static string GetOrderedCustomers(CarDealerContext context)
        {
            //Get all customers ordered by their birth date ascending. If two customers are born on the same date first print those who are not young drivers (e.g. print experienced drivers first). Export the list of customers to JSON in the format provided below.

            var customers = context.Customers
                .OrderBy(c => c.BirthDate)
                .ThenBy(c => c.IsYoungDriver)
                .Select(c => new
                {
                    Name = c.Name,
                    BirthDate = c.BirthDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    IsYoungDriver = c.IsYoungDriver
                })
                .ToArray();

            return JsonConvert.SerializeObject(customers, Formatting.Indented);
        }

        public static string ImportSales(CarDealerContext context, string inputJson)
        {
            var customers = context.Customers
                .Where(c => c.IsYoungDriver)
                .Select(c => c.Id)
                .ToArray();

            var sales = JsonConvert.DeserializeObject<Sale[]>(inputJson).ToArray();

            foreach (var sale in sales)
            {
                if (customers.Contains(sale.CustomerId))
                {
                    sale.Discount += 5;
                }
            }
            ;
            context.Sales.AddRange(sales);
            context.SaveChanges();

            return $"Successfully imported {sales.Length}.";
        }

        public static string ImportCustomers(CarDealerContext context, string inputJson)
        {
            var customers = JsonConvert.DeserializeObject<Customer[]>(inputJson);

            context.Customers.AddRange(customers);
            context.SaveChanges();

            return $"Successfully imported {customers.Length}.";
        }

        public static string ImportCars(CarDealerContext context, string inputJson)
        {
            var carDtos = JsonConvert.DeserializeObject<CarDto[]>(inputJson);

            var cars = new List<Car>();
            var partsCar = new List<PartCar>();

            var partsCount = context.Parts.Count();

            foreach (var carDto in carDtos)
            {
                var car = new Car()
                {
                    Make = carDto.Make,
                    Model = carDto.Model,
                    TravelledDistance = carDto.TravelledDistance
                };

                foreach (var partId in carDto.PartsId.Distinct())
                {
                    if (partId <= partsCount)
                    {
                        partsCar.Add(new PartCar()
                        {
                            Car = car,
                            PartId = partId
                        });
                    }
                }

                cars.Add(car);
            }

            context.Cars.AddRange(cars);

            var seededCarsLength = context.SaveChanges();

            context.PartCars.AddRange(partsCar);

            context.SaveChanges();

            return $"Successfully imported {seededCarsLength}.";
        }

        public static string ImportParts(CarDealerContext context, string inputJson)
        {
            var suppliersIds = context.Suppliers
                .Select(s => s.Id)
                .ToArray();

            var parts = JsonConvert.DeserializeObject<Part[]>(inputJson)
                .Where(p => suppliersIds.Contains(p.SupplierId))
                .ToArray();

            context.Parts.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {parts.Length}.";
        }

        public static string ImportSuppliers(CarDealerContext context, string inputJson)
        {
            var suppliers = JsonConvert.DeserializeObject<Supplier[]>(inputJson);

            context.Suppliers.AddRange(suppliers);
            context.SaveChanges();

            return $"Successfully imported {suppliers.Length}.";
        }
    }
}