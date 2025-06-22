using ETLFramework.Playground.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ETLFramework.Playground.Services;

/// <summary>
/// Implementation of sample data generation service.
/// </summary>
public class SampleDataService : ISampleDataService
{
    private readonly ILogger<SampleDataService> _logger;
    private readonly Random _random = new();
    private readonly List<string> _tempFiles = new();

    // Sample data arrays
    private readonly string[] _firstNames = {
        "John", "Jane", "Michael", "Sarah", "David", "Emily", "Robert", "Jessica",
        "William", "Ashley", "James", "Amanda", "Christopher", "Stephanie", "Daniel",
        "Melissa", "Matthew", "Nicole", "Anthony", "Elizabeth", "Mark", "Helen",
        "Donald", "Deborah", "Steven", "Rachel", "Paul", "Carolyn", "Andrew", "Janet"
    };

    private readonly string[] _lastNames = {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
        "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
        "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson"
    };

    private readonly string[] _cities = {
        "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia",
        "San Antonio", "San Diego", "Dallas", "San Jose", "Austin", "Jacksonville",
        "Fort Worth", "Columbus", "Charlotte", "San Francisco", "Indianapolis",
        "Seattle", "Denver", "Washington", "Boston", "El Paso", "Nashville",
        "Detroit", "Oklahoma City", "Portland", "Las Vegas", "Memphis", "Louisville"
    };

    private readonly string[] _states = {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA", "HI", "ID",
        "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS",
        "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK",
        "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
    };

    private readonly string[] _productCategories = {
        "Electronics", "Clothing", "Home & Garden", "Sports & Outdoors", "Books",
        "Health & Beauty", "Automotive", "Toys & Games", "Food & Beverages", "Office Supplies"
    };

    private readonly string[] _departments = {
        "Engineering", "Sales", "Marketing", "Human Resources", "Finance", "Operations",
        "Customer Service", "IT", "Legal", "Research & Development"
    };

    private readonly string[] _jobTitles = {
        "Software Engineer", "Sales Representative", "Marketing Manager", "HR Specialist",
        "Financial Analyst", "Operations Manager", "Customer Service Rep", "IT Administrator",
        "Legal Counsel", "Research Scientist", "Product Manager", "Business Analyst",
        "Quality Assurance", "Data Scientist", "Project Manager"
    };

    public SampleDataService(ILogger<SampleDataService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<CustomerData> GenerateCustomerData(int count = 100)
    {
        _logger.LogDebug("Generating {Count} customer records", count);

        for (int i = 1; i <= count; i++)
        {
            var firstName = _firstNames[_random.Next(_firstNames.Length)];
            var lastName = _lastNames[_random.Next(_lastNames.Length)];
            var city = _cities[_random.Next(_cities.Length)];
            var state = _states[_random.Next(_states.Length)];

            yield return new CustomerData
            {
                CustomerId = i,
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                Phone = GeneratePhoneNumber(),
                DateOfBirth = GenerateRandomDate(DateTime.Now.AddYears(-80), DateTime.Now.AddYears(-18)),
                Address = $"{_random.Next(100, 9999)} {GenerateStreetName()}",
                City = city,
                State = state,
                ZipCode = _random.Next(10000, 99999).ToString(),
                Country = "USA",
                CreatedDate = GenerateRandomDate(DateTime.Now.AddYears(-5), DateTime.Now),
                IsActive = _random.NextDouble() > 0.1, // 90% active
                CustomerType = _random.NextDouble() > 0.3 ? "Regular" : "Premium",
                CreditLimit = _random.Next(1000, 50000)
            };
        }
    }

    /// <inheritdoc />
    public IEnumerable<ProductData> GenerateProductData(int count = 50)
    {
        _logger.LogDebug("Generating {Count} product records", count);

        for (int i = 1; i <= count; i++)
        {
            var category = _productCategories[_random.Next(_productCategories.Length)];
            var productName = GenerateProductName(category);
            var cost = _random.Next(10, 500);
            var price = cost * (decimal)(_random.NextDouble() * 2 + 1.5); // 1.5x to 3.5x markup

            yield return new ProductData
            {
                ProductId = i,
                ProductName = productName,
                SKU = $"SKU-{i:D6}",
                Category = category,
                Subcategory = GenerateSubcategory(category),
                Price = Math.Round(price, 2),
                Cost = cost,
                StockQuantity = _random.Next(0, 1000),
                Description = $"High-quality {productName.ToLower()} for everyday use",
                Brand = GenerateBrandName(),
                Weight = Math.Round(_random.NextDouble() * 10 + 0.1, 2),
                Dimensions = $"{_random.Next(5, 50)}x{_random.Next(5, 50)}x{_random.Next(5, 50)} cm",
                CreatedDate = GenerateRandomDate(DateTime.Now.AddYears(-3), DateTime.Now),
                DiscontinuedDate = _random.NextDouble() > 0.9 ? GenerateRandomDate(DateTime.Now.AddYears(-1), DateTime.Now) : null,
                IsActive = _random.NextDouble() > 0.05 // 95% active
            };
        }
    }

    /// <inheritdoc />
    public IEnumerable<OrderData> GenerateOrderData(int count = 200)
    {
        _logger.LogDebug("Generating {Count} order records", count);

        var statuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
        var paymentMethods = new[] { "Credit Card", "Debit Card", "PayPal", "Bank Transfer", "Cash" };

        for (int i = 1; i <= count; i++)
        {
            var orderDate = GenerateRandomDate(DateTime.Now.AddYears(-2), DateTime.Now);
            var subtotal = (decimal)(_random.NextDouble() * 1000 + 50);
            var taxAmount = subtotal * 0.08m; // 8% tax
            var shippingAmount = (decimal)(_random.NextDouble() * 20 + 5);
            var totalAmount = subtotal + taxAmount + shippingAmount;

            var city = _cities[_random.Next(_cities.Length)];
            var state = _states[_random.Next(_states.Length)];

            yield return new OrderData
            {
                OrderId = i,
                CustomerId = _random.Next(1, 101), // Assuming 100 customers
                OrderDate = orderDate,
                ShippedDate = _random.NextDouble() > 0.2 ? orderDate.AddDays(_random.Next(1, 7)) : null,
                OrderStatus = statuses[_random.Next(statuses.Length)],
                SubTotal = Math.Round(subtotal, 2),
                TaxAmount = Math.Round(taxAmount, 2),
                ShippingAmount = Math.Round(shippingAmount, 2),
                TotalAmount = Math.Round(totalAmount, 2),
                ShippingAddress = $"{_random.Next(100, 9999)} {GenerateStreetName()}",
                ShippingCity = city,
                ShippingState = state,
                ShippingZipCode = _random.Next(10000, 99999).ToString(),
                PaymentMethod = paymentMethods[_random.Next(paymentMethods.Length)],
                Notes = _random.NextDouble() > 0.7 ? "Special delivery instructions" : string.Empty
            };
        }
    }

    /// <inheritdoc />
    public IEnumerable<EmployeeData> GenerateEmployeeData(int count = 75)
    {
        _logger.LogDebug("Generating {Count} employee records", count);

        for (int i = 1; i <= count; i++)
        {
            var firstName = _firstNames[_random.Next(_firstNames.Length)];
            var lastName = _lastNames[_random.Next(_lastNames.Length)];
            var department = _departments[_random.Next(_departments.Length)];
            var jobTitle = _jobTitles[_random.Next(_jobTitles.Length)];
            var hireDate = GenerateRandomDate(DateTime.Now.AddYears(-10), DateTime.Now.AddMonths(-1));

            yield return new EmployeeData
            {
                EmployeeId = i,
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@company.com",
                Phone = GeneratePhoneNumber(),
                HireDate = hireDate,
                Department = department,
                JobTitle = jobTitle,
                Salary = _random.Next(40000, 150000),
                ManagerId = i > 10 ? _random.Next(1, i) : null, // Some employees have managers
                OfficeLocation = _cities[_random.Next(_cities.Length)],
                IsActive = _random.NextDouble() > 0.05, // 95% active
                TerminationDate = _random.NextDouble() > 0.95 ? hireDate.AddYears(_random.Next(1, 5)) : null,
                EmployeeType = _random.NextDouble() > 0.2 ? "Full-time" : "Part-time"
            };
        }
    }

    /// <inheritdoc />
    public IEnumerable<ProblematicData> GenerateProblematicData(int count = 50)
    {
        _logger.LogDebug("Generating {Count} problematic data records", count);

        for (int i = 1; i <= count; i++)
        {
            yield return new ProblematicData
            {
                Id = _random.NextDouble() > 0.1 ? i.ToString() : null, // 10% null IDs
                Name = GenerateProblematicName(),
                Email = GenerateProblematicEmail(),
                Phone = GenerateProblematicPhone(),
                Age = GenerateProblematicAge(),
                Salary = GenerateProblematicSalary(),
                Date = GenerateProblematicDate(),
                Status = GenerateProblematicStatus(),
                Notes = _random.NextDouble() > 0.5 ? "Some notes here" : null
            };
        }
    }

    // Helper methods for data generation
    private DateTime GenerateRandomDate(DateTime start, DateTime end)
    {
        var range = end - start;
        var randomDays = _random.Next(0, range.Days + 1);
        return start.AddDays(randomDays);
    }

    private string GeneratePhoneNumber()
    {
        return $"({_random.Next(200, 999)}) {_random.Next(200, 999)}-{_random.Next(1000, 9999)}";
    }

    private string GenerateStreetName()
    {
        var streetNames = new[] { "Main St", "Oak Ave", "Pine Rd", "Elm Dr", "Maple Ln", "Cedar Ct", "Park Blvd", "First St" };
        return streetNames[_random.Next(streetNames.Length)];
    }

    private string GenerateProductName(string category)
    {
        var adjectives = new[] { "Premium", "Deluxe", "Professional", "Standard", "Economy", "Ultra", "Super", "Advanced" };
        var nouns = category switch
        {
            "Electronics" => new[] { "Smartphone", "Laptop", "Tablet", "Headphones", "Speaker", "Camera" },
            "Clothing" => new[] { "Shirt", "Pants", "Jacket", "Dress", "Shoes", "Hat" },
            "Home & Garden" => new[] { "Chair", "Table", "Lamp", "Vase", "Plant", "Tool" },
            _ => new[] { "Product", "Item", "Device", "Gadget", "Tool", "Accessory" }
        };

        var adjective = adjectives[_random.Next(adjectives.Length)];
        var noun = nouns[_random.Next(nouns.Length)];
        return $"{adjective} {noun}";
    }

    private string GenerateSubcategory(string category)
    {
        return category switch
        {
            "Electronics" => new[] { "Mobile", "Computing", "Audio", "Gaming" }[_random.Next(4)],
            "Clothing" => new[] { "Men's", "Women's", "Children's", "Accessories" }[_random.Next(4)],
            "Home & Garden" => new[] { "Furniture", "Decor", "Tools", "Outdoor" }[_random.Next(4)],
            _ => "General"
        };
    }

    private string GenerateBrandName()
    {
        var brands = new[] { "TechCorp", "StyleCo", "HomeMax", "SportsPro", "QualityPlus", "ValueBrand", "PremiumLine", "EcoFriendly" };
        return brands[_random.Next(brands.Length)];
    }

    // Problematic data generators
    private string? GenerateProblematicName()
    {
        var options = new[]
        {
            null, // null name
            "", // empty name
            "   ", // whitespace only
            "a", // too short
            new string('x', 100), // too long
            "John123", // contains numbers
            "John@Doe", // contains special characters
            "john doe" // normal case for comparison
        };

        return options[_random.Next(options.Length)];
    }

    private string? GenerateProblematicEmail()
    {
        var options = new[]
        {
            null,
            "",
            "invalid-email",
            "missing@domain",
            "@missing-local.com",
            "spaces in@email.com",
            "valid@email.com" // some valid ones
        };

        return options[_random.Next(options.Length)];
    }

    private string? GenerateProblematicPhone()
    {
        var options = new[]
        {
            null,
            "",
            "123", // too short
            "not-a-phone-number",
            "123-456-78901", // too long
            "(555) 123-4567" // valid format
        };

        return options[_random.Next(options.Length)];
    }

    private string? GenerateProblematicAge()
    {
        var options = new[]
        {
            null,
            "",
            "-5", // negative
            "999", // too high
            "not-a-number",
            "25.5", // decimal
            "30" // valid
        };

        return options[_random.Next(options.Length)];
    }

    private string? GenerateProblematicSalary()
    {
        var options = new[]
        {
            null,
            "",
            "-1000", // negative
            "not-a-salary",
            "1,000,000", // with commas
            "$50000", // with currency symbol
            "50000" // valid
        };

        return options[_random.Next(options.Length)];
    }

    private string? GenerateProblematicDate()
    {
        var options = new[]
        {
            null,
            "",
            "not-a-date",
            "2023-13-45", // invalid date
            "31/12/2023", // wrong format
            "2023-12-31T25:00:00", // invalid time
            "2023-12-31" // valid
        };

        return options[_random.Next(options.Length)];
    }

    private string? GenerateProblematicStatus()
    {
        var options = new[]
        {
            null,
            "",
            "UNKNOWN_STATUS",
            "Active",
            "Inactive",
            "Pending"
        };

        return options[_random.Next(options.Length)];
    }

    /// <inheritdoc />
    public async Task<string> CreateTempCsvFileAsync<T>(IEnumerable<T> data, string? fileName = null)
    {
        fileName ??= $"temp_data_{Guid.NewGuid():N}.csv";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        var csv = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Add header
        csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        // Add data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                var stringValue = value?.ToString() ?? "";
                // Escape commas and quotes in CSV
                if (stringValue.Contains(',') || stringValue.Contains('"'))
                {
                    stringValue = $"\"{stringValue.Replace("\"", "\"\"")}\"";
                }
                return stringValue;
            });
            csv.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(filePath, csv.ToString());
        _tempFiles.Add(filePath);
        _logger.LogDebug("Created temporary CSV file: {FilePath}", filePath);

        return filePath;
    }

    /// <inheritdoc />
    public async Task<string> CreateTempJsonFileAsync<T>(IEnumerable<T> data, string? fileName = null)
    {
        fileName ??= $"temp_data_{Guid.NewGuid():N}.json";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync(filePath, json);
        _tempFiles.Add(filePath);
        _logger.LogDebug("Created temporary JSON file: {FilePath}", filePath);

        return filePath;
    }

    /// <inheritdoc />
    public void CleanupTempFiles()
    {
        foreach (var filePath in _tempFiles.ToList())
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug("Deleted temporary file: {FilePath}", filePath);
                }
                _tempFiles.Remove(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary file: {FilePath}", filePath);
            }
        }
    }

    /// <summary>
    /// Finalizer to ensure temp files are cleaned up.
    /// </summary>
    ~SampleDataService()
    {
        CleanupTempFiles();
    }
}
