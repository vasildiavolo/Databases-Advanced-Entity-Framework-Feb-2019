namespace CarDealer
{
    using AutoMapper;
    using CarDealer.Data;
    using CarDealer.Dtos.Export;
    using CarDealer.Dtos.Import;
    using CarDealer.IO.Writers;
    using CarDealer.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    public class StartUp
    {
        public static void Main(string[] args)
        {
            //Mapper.Initialize(m =>
            //{
            //    m.AddProfile<CarDealerProfile>();
            //});

            //string xml = File.ReadAllText(@"C:\Users\Vasil Evlogiev\source\repos\Exercise XML Processing\CarDealerXML\CarDealer\Datasets\sales.xml");

            using (var context = new CarDealerContext())
            {
                var result = GetSalesWithAppliedDiscount(context);
                Console.WriteLine(result);
            }
        }

        public static string GetSalesWithAppliedDiscount(CarDealerContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportSaleDto[]), new XmlRootAttribute("sales"));

            var sales = context.Sales
               .Select(x => new ExportSaleDto()
                {
                   Car = new ExportCarInSaleDto()
                   {
                       Make = x.Car.Make,
                       Model = x.Car.Model,
                       TravelledDistance = x.Car.TravelledDistance
                   },
                   Discount = decimal.Parse(x.Discount.ToString("f0")),
                   CustomerName = x.Customer.Name,
                   Price = x.Car.PartCars.Sum(y => y.Part.Price),
                   PriceWithDiscount = (x.Car.PartCars
                                .Sum(pc => pc.Part.Price) -
                                (x.Car.PartCars.Sum(pc => pc.Part.Price) *
                                x.Discount / 100m))
               })
                .ToArray();

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            serializer.Serialize(new StringWriter(builder), sales, namespaces);

            return builder.ToString();
        }

        public static string GetTotalSalesByCustomer(CarDealerContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportCustomerDto[]), new XmlRootAttribute("customers"));

            var customers = context.Customers
                .Where(c => c.Sales.Count > 0)
                .Select(c => new ExportCustomerDto
                {
                    FullName = c.Name,
                    BoughtCars = c.Sales.Count,
                    SpentMoney = c.Sales.Select(s => s.Car.PartCars.Select(pc => pc.Part.Price).Sum()).Sum()
                })
                .OrderByDescending(x => x.SpentMoney)
                .ToArray();
            ;

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            serializer.Serialize(new StringWriter(builder), customers, namespaces);

            return builder.ToString();
        }

        public static string GetCarsWithTheirListOfParts(CarDealerContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportCarAndPartsDto[]), new XmlRootAttribute("cars"));

            var cars = context.Cars
                .Select(c => new ExportCarAndPartsDto
                {
                    Make = c.Make,
                    Model = c.Model,
                    TravelledDistance = c.TravelledDistance,

                    Parts = c.PartCars
                    .Select(pc => pc.Part)
                    .Select(p => new ExportPartDto
                    {
                        Name = p.Name,
                        Price = p.Price
                    })
                    .OrderByDescending(epdto => epdto.Price)
                    .ToArray()
                })
                .OrderByDescending(epdto => epdto.TravelledDistance)
                .ThenBy(epdto => epdto.Model)
                .Take(5)
                .ToArray();

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            serializer.Serialize(new StringWriter(builder), cars, namespaces);

            return builder.ToString();
        }

        public static string GetLocalSuppliers(CarDealerContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportSupplierDto[]), new XmlRootAttribute("suppliers"));

            var suppliers = context.Suppliers
                .Where(s => s.IsImporter == false)
                .Select(s => new ExportSupplierDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    PartsCount = s.Parts.Count
                })
                .ToArray();

            StringBuilder builder = new StringBuilder();
            var writer = new StringWriterUTF8(builder);
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            serializer.Serialize(writer, suppliers, namespaces);

            return builder.ToString();

        }

        public static string GetCarsFromMakeBmw(CarDealerContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportCarBMWDto[]), new XmlRootAttribute("cars"));

            var cars = context.Cars
                .Where(c => c.Make == "BMW")
                .OrderBy(c => c.Model)
                .ThenByDescending(c => c.TravelledDistance)
                .Select(c => new ExportCarBMWDto
                {
                    Id = c.Id,
                    Model = c.Model,
                    TravelledDistance = c.TravelledDistance
                })
                .ToArray();

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            serializer.Serialize(new StringWriter(builder), cars, namespaces);

            return builder.ToString();
        }

        public static string GetCarsWithDistance(CarDealerContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportCarDto[]), new XmlRootAttribute("cars"));

            var cars = context.Cars
                .Where(c => c.TravelledDistance > 2000000)
                .Select(c => new ExportCarDto
                {
                    Make = c.Make,
                    Model = c.Model,
                    TravelledDistance = c.TravelledDistance
                })
                .OrderBy(ecdto => ecdto.Make)
                .ThenBy(ecdto => ecdto.Model)
                .Take(10)
                .ToArray();

            StringBuilder builder = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            serializer.Serialize(new StringWriter(builder), cars, namespaces);

            return builder.ToString();
        }

        public static string ImportSales(CarDealerContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportSaleDto[]), new XmlRootAttribute("Sales"));

            var salesDto = (ImportSaleDto[])serializer.Deserialize(new StringReader(inputXml));

            var sales = new List<Sale>();
            var carsCount = context.Cars.Count();

            foreach (var saleDto in salesDto)
            {
                if (saleDto.CarId <= carsCount)
                {
                    var sale = Mapper.Map<Sale>(saleDto);
                    sales.Add(sale);
                }
            }
            ;
            context.Sales.AddRange(sales);
            context.SaveChanges();

            return $"Successfully imported {sales.Count}";
        }

        public static string ImportCustomers(CarDealerContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportCustomerDto[]), new XmlRootAttribute("Customers"));

            var customersDto = (ImportCustomerDto[])serializer.Deserialize(new StringReader(inputXml));

            var customers = new List<Customer>();

            foreach (var customerDto in customersDto)
            {
                var cusomer = Mapper.Map<Customer>(customerDto);
                customers.Add(cusomer);
            }

            context.Customers.AddRange(customers);
            context.SaveChanges();

            return $"Successfully imported {customers.Count}";
        }

        public static string ImportCars(CarDealerContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportCarDto[]), new XmlRootAttribute("Cars"));

            var carsDto = (ImportCarDto[])serializer.Deserialize(new StringReader(inputXml));

            var cars = new List<Car>();
            var partsCar = new List<PartCar>();

            var partsCount = context.Parts.Count();

            foreach (var carDto in carsDto)
            {
                var car = new Car()
                {
                    Make = carDto.Make,
                    Model = carDto.Model,
                    TravelledDistance = carDto.TraveledDistance
                };

                var currentPartsIds = carDto.PartsId.Select(x => x.Id).Distinct().ToArray();

                foreach (var partId in currentPartsIds)
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
            context.SaveChanges();

            context.PartCars.AddRange(partsCar);
            context.SaveChanges();

            return $"Successfully imported {cars.Count}";
        }

        public static string ImportParts(CarDealerContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportPartDto[]), new XmlRootAttribute("Parts"));

            var partsDto = (ImportPartDto[])serializer.Deserialize(new StringReader(inputXml));

            var parts = new List<Part>();
            var suppliersId = context.Suppliers.Select(s => s.Id).ToArray();

            foreach (var partDto in partsDto)
            {
                if (suppliersId.Contains(partDto.SupplierId))
                {
                    var part = Mapper.Map<Part>(partDto);
                    parts.Add(part);
                }
            }

            context.Parts.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {parts.Count}";
        }

        public static string ImportSuppliers(CarDealerContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ImportSupplierDto[]), new XmlRootAttribute("Suppliers"));

            var suppliersDto = (ImportSupplierDto[])serializer.Deserialize(new StringReader(inputXml));

            var suppliers = new List<Supplier>();

            foreach (var supplierDto in suppliersDto)
            {
                var supplier = Mapper.Map<Supplier>(supplierDto);
                suppliers.Add(supplier);
            }

            context.Suppliers.AddRange(suppliers);
            context.SaveChanges();

            return $"Successfully imported {suppliers.Count}";
        }
    }
}