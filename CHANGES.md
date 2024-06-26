## Project Development Procces Tracker:

#### Project: Online store web application as a part of a web development course.

Goal: To track progress I embedded all of the development process(code edits, file creation, design pattern implementation etc.) in this file so in the end I will have whole development procces documented.

### Stack: .NET 5.0, Angular 11 and SQLite.

### Enviroment: Windows 10

### Date Created: 30.7.2021.

### Last Edit: 19.8.2021.

### Contributors: Emin Kočan

# API:

## 1. API Basics:

### 1.1. Made a Skeleton API

### Created project and structured it so it is split in 3 parts:

- API: Main project in which we store controllers, `Program.cs` , `Startup.cs` & configuration files.
- Core: A place where we will store our `Entities` & `Interfaces`.
- Infrastructure: This is where all data related parts of application like Entity Framework configuration files, migrations, .json data files, and other classes like Repository & Data Context classes will be stored.

## 2. API Architecture:

### 2.1. Repository Pattern:

### 2.2. Added Repository Methods:

1.  Added classes: `IProductRepository.cs` in `Interfaces`, `ProductRepository.cs` in `Data`
2.  Declared following methods in `IProductRepository.cs`:

```c#
//..Interfaces/IProductRepository.cs
Task<Product> GetProductByIdAsync(int id);
Task<IReadOnlyList<Product>> GetProductsAsync();
```

3.  Added ProductRepository as a service in `Startup.cs`:

```c#
//..API/Startup.cs
 public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddDbContext<StoreContext>(x =>
            {
                x.UseSqlite(_config.GetConnectionString("DefaultConnection"));
            });
            //->Here
            services.AddScoped<IProductRepository, ProductRepository>();
        }
```

4.  Create a constructor `ProductRepository(Storecontext context)` to take readonly StoreContext variable on initial creation:

```c#
 private readonly StoreContext _context;

        public ProductRepository(StoreContext context)
        {
            _context = context;
        }
```

5.  Implemented following ProductRepository methods:

```c#
public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IReadOnlyList<Product>> GetProductsAsync()
        {
            return await _context.Products
                .ToListAsync();
        }
```

### 2.3. Extended the products entity and created related entities

```C#
//BaseEntity.cs
namespace Core.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
    }
}
```

```c#
//ProductBrand.cs
namespace Core.Entities
{
    public class ProductBrand:BaseEntity
    {
        public string Name { get; set; }
    }
}
```

```c#
//ProductType.cs
namespace Core.Entities
{
    public class ProductType:BaseEntity
    {
        public string Name { get; set; }
    }
}
```

```c#
//Product.cs
using System.Text;

namespace Core.Entities
{
    public class Product:BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string PictureUrl { get; set; }
        public ProductType ProductType { get; set; }
        public int ProductTypeId { get; set; }
        public ProductBrand ProductBrand { get; set; }
        public int ProductBrandId { get; set; }

    }
}
```

### 2.4. Creating a new migration for the entities:

```c#
//Dropping the old database:
dotnet ef migrations remove -p Infrastructure -s API
//Removing the old migrations:
dotnet ef migrations remove -p Infrastructure -s API
//Creating new initial migration:
dotnet ef migrations add InitialCreate -p Infrastructure -s API -o Data/Migrations
```

### 2.5. Configuring migrations

1.  Created new directory `Config` and added `ProductConfiguration.cs` class
2.  Derived `ProductConfiguration` class from `IEntityTypeConfiguration<>` interface

```c#
//Data/Config/ProductConfiguration.cs
public class ProductConfiguration:IEntityTypeConfiguration<Product>
```

3.  Implemented `Configure` method:

```c#
//..Data/Config/ProductConfiguration.cs
public void Configure(EntityTypeBuilder<Product> builder)
{
builder.Property(p => p.Id).IsRequired();
builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
builder.Property(p => p.Description).IsRequired();
builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
builder.Property(p => p.PictureUrl).IsRequired();
builder.HasOne(b => b.ProductBrand).WithMany().HasForeignKey(p => p.ProductBrandId);
builder.HasOne(t => t.ProductType).WithMany().HasForeignKey(p => p.ProductTypeId);
}
```

```c#
//..Data/StoreContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

```

### 2.6. Configuring Startup.cs to Create the database if missing and sync with migrations on app startup:

```c#
//..API/Program.cs
public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                try
                {
                    var context = services.GetRequiredService<StoreContext>();
                    //await context.Database.MigrateAsync();
                    //await StoreContextSeed.SeedAsync(context,loggerFactory);
                }
                catch (Exception ex)
                {
                    var logger = loggerFactory.CreateLogger<Program>();
                    logger.LogError(ex,"An error occured during migration");
                }
            }
            host.Run();
        }
```

### 2.7. Added Seed Data from files:

#### 1. Created new directory `Data/SeedData` with and added files `brands.json` `types.json` & `products.json`

#### 2. Added New Class `Data/StoreContextSeed.cs`

```c#
//..Data/StoreContext.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{
    public class StoreContextSeed
    {
        public static async Task SeedAsync(StoreContext context, ILoggerFactory loggerFactory)
        {
            try
            {
                if (!context.ProductBrands.Any())
                {
                    var brandsData = File.ReadAllText("../Infrastructure/Data/SeedData/brands.json");
                    var brands = JsonSerializer.Deserialize<List<ProductBrand>>(brandsData);
                    foreach (var item in brands)
                    {
                        context.ProductBrands.Add(item);
                    }
                    await context.SaveChangesAsync();
                }
                if (!context.ProductTypes.Any())
                {
                    var typesData = File.ReadAllText("../Infrastructure/Data/SeedData/types.json");
                    var types = JsonSerializer.Deserialize<List<ProductType>>(typesData);
                    foreach (var item in types)
                    {
                        context.ProductTypes.Add(item);
                    }
                    await context.SaveChangesAsync();
                }
                if (!context.Products.Any())
                {
                    var productsData = File.ReadAllText("../Infrastructure/Data/SeedData/products.json");
                    var products = JsonSerializer.Deserialize<List<Product>>(productsData);
                    foreach (var item in products)
                    {
                        context.Products.Add(item);
                    }
                    await context.SaveChangesAsync();
                }
            }
            catch(Exception ex)
            {
                var logger = loggerFactory.CreateLogger<StoreContextSeed>();
                logger.LogError(ex.Message);
            }
        }
    }
}
```

#### 3. Updated `Program.cs`

```c#
 try
{
var context = services.GetRequiredService<StoreContext>();
//Next two lines
await context.Database.MigrateAsync();
await StoreContextSeed.SeedAsync(context,loggerFactory);
}
```

### 2.8. Refactoring code to get product brands and types along with Product object

#### 1. Updated `IProductRepository.cs`

```c#
//..Core/Interfaces/IProductRepository.cs
//Added following methods:
Task<IReadOnlyList<ProductBrand>> GetProductsBrandsAsync();
Task<IReadOnlyList<ProductType>> GetProductsTypesAsync();
```

#### 2. Implemented methods in `ProductRepository.cs`

```c#
public async Task<IReadOnlyList<ProductBrand>> GetProductsBrandsAsync()
        {
            return await _context.ProductBrands.ToListAsync();
        }

public async Task<IReadOnlyList<ProductType>> GetProductsTypesAsync()
        {
            return await _context.ProductTypes.ToListAsync();
        }
```

#### 3. Updated `ProductController.cs`

```c#
//..API/Controllers/ProductController.cs
 [HttpGet("brands")]
        public async Task<ActionResult<IReadOnlyList<ProductBrand>>> GetProductBrands()
        {
            return Ok(await _repo.GetProductsBrandsAsync());
        }
        [HttpGet("types")]
        public async Task<ActionResult<IReadOnlyList<ProductType>>> GetProductTypes()
        {
            return Ok(await _repo.GetProductsTypesAsync());
        }
```

### 2.9. Added Eager Loading to navigation properties

```c#
// ..Infrastructure/ProductRepository.cs
public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products
            //added next two lines
                .Include(p => p.ProductType)
                .Include(p => p.ProductBrand)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

public async Task<IReadOnlyList<Product>> GetProductsAsync()
        {
            return await _context.Products
            //added next two lines
                .Include(p=>p.ProductType)
                .Include(p=>p.ProductBrand)
                .ToListAsync();
        }
```

### 2.10. Updated `README.md`

## 3. API Generic Repository

#### In this part i will cover:

#### - Creating a generic repository

#### - Specification pattern

#### - Using the specification pattern

#### - Shape data

#### - AutoMapper

#### - Serving static content from API

### 3.1. Creating a Generic repository and interface

#### Created new class in interfaces folder called IGenericRepository

```c#
//../Interfaces/IGenericRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T> GetByIdAsync(int id);
        Task<IReadOnlyList<T>> ListAllAsync();
    }
}
```

#### Created `GenericRepository.cs` class in `Infrastructure/Data` folder

### 3.2. Implementing methods in Generic Repository

#### Updated `ConfigureServices` method in `Startup.cs`

```c#
public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
//---------------------------------------------------------------------------------------------
            services.AddScoped(typeof(IGenericRepository<>), (typeof(GenericRepository<>)));
//---------------------------------------------------------------------------------------------
            services.AddControllers();
            services.AddDbContext<StoreContext>(x =>
                x.UseSqlite(_config.GetConnectionString("DefaultConnection")));

        }
```

#### Updated `GenericRepository.cs`

```c#
//..Infrastructure/Data/GenericRepository.cs
namespace Infrastructure.Data
{
    public class GenericRepository<T>:IGenericRepository<T> where T : BaseEntity
    {
        private readonly StoreContext _context;

        public GenericRepository(StoreContext context)
        {
            _context = context;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> ListAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }
    }
}
```

#### Updated `ProductController.cs`

```c#
//..API/Startup.cs
namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IGenericRepository<Product> _productsRepo;
        private readonly IGenericRepository<ProductBrand> _productBrandRepo;
        private readonly IGenericRepository<ProductType> _productTypeRepo;

        public ProductsController(IGenericRepository<Product> productsRepo,
            IGenericRepository<ProductBrand> productBrandRepo, IGenericRepository<ProductType> productTypeRepo)
        {
            _productsRepo = productsRepo;
            _productBrandRepo = productBrandRepo;
            _productTypeRepo = productTypeRepo;
        }
        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetProducts()
        {
            var products = await _productsRepo.ListAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<List<Product>>> GetProduct(int id)
        {
            return Ok(await _productsRepo.GetByIdAsync(id));
        }

        [HttpGet("brands")]
        public async Task<ActionResult<IReadOnlyList<ProductBrand>>> GetProductBrands()
        {
            return Ok(await _productBrandRepo.ListAllAsync());
        }
        [HttpGet("types")]
        public async Task<ActionResult<IReadOnlyList<ProductType>>> GetProductTypes()
        {
            return Ok(await _productTypeRepo.ListAllAsync());
        }
    }
}
```

### 3.3. Specification Pattern:

##### Whats the use:

##### - Describes a query in an object

##### - Returns an IQueryable<T>

##### - Generic List method takes specification as parameter

##### - Specification can have meaningful name

### 3.3.1. Creating a Specification Evaluator

1.  Create new `Specifications` directory in `Core` project
2.  Create new `ISpecification.cs` interface in `Specifications` dir

```c#
//..Specifications/ISpecification.cs
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace API.Specifications
{
   public interface ISpecification<T>
   {
       Expression<Func<T, bool>> Criteria { get; }
       List<Expression<Func<T, object>>> Includes { get; }
   }
}
```

3.  Create a new class `BaseSpecification.cs` in `Specifications`

```c#
 //..Specifications/BaseSpecification.cs
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace API.Specifications
{
    public class BaseSpecification<T> : ISpecification<T>
    {
        public BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public Expression<Func<T, bool>> Criteria { get; }
        public List<Expression<Func<T, object>>> Includes { get; } = new List<Expression<Func<T, object>>>();

        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }
    }
}
```

4. In `Data` create a new c# class `SpecificationEvaluator.cs`

```c#
//..Infrastructure/Data/SpecificationEvaluator.cs
using System.Linq;
using API.Specifications;
using Core.Entities;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Data
{
    public class SpecificationEvaluator<TEntity> where TEntity : BaseEntity
    {
        public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> inputQuery, ISpecification<TEntity> spec)
        {
            var query = inputQuery;
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
            return query;
        }
    }
}
```

5.  Added following methods in `IGenericRepository.cs`

```c#
Task<T> GetEntityWithSpec(ISpecification<T> spec);
Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);
```

### 3.4. Implementing the repository with specification methods

Added following methods in `GenericRepository.cs`

```c#
public async Task<T> GetEntityWithSpec(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
        }

private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), spec);
        }
```

### 3.5. Using the specification methods in controller

1.  Generate an empty constructor in `BaseSpecification.cs`
2.  Create `ProductsWithTypesAndBrandsSpecification.cs` in `Specifications`

```c#
using Core.Entities;

namespace API.Specifications
{
    public class ProductsWithTypesAndBrandsSpecification : BaseSpecification<Product>
    {
        public ProductsWithTypesAndBrandsSpecification()
        {
            AddInclude(x => x.ProductType);
            AddInclude(x => x.ProductBrand);
        }
    }
}
```

3. Edit `GetProducts()` method to now show include ProductType and ProductBrand properties along with Product objects.

```c#
// This is where we return product list but until this change Product object would be returned without productType and productBrand properties.
// In order to do so, instead of using var products = await _productsRepo.ListAllAsync(); code was refactored to following:
public async Task<ActionResult<List<Product>>> GetProducts()
        {
            // Create specification using ProductWithTypesAndBrandsSpec
            var spec = new ProductsWithTypesAndBrandsSpecification();
            // Use spec variable as argument in .ListAsync method
            var products = await _productsRepo.ListAsync(spec);
            return Ok(products);
        }
```

### 3.6. Using specific methods in controller

1.  Overload a constructor in `BaseSpecification.cs` to take `criteria` as a paremeter:

```c#
public BaseSpecification(Expression<Func<T, bool>> criteria)
{
    Criteria = criteria;
}
```

2. Overload ProductsWithTypesAndBrandsSpecification class constructor:

```c#
public ProductsWithTypesAndBrandsSpecification(int id) : base(x=>x.Id==id)
{
    AddInclude(x => x.ProductType);
    AddInclude(x => x.ProductBrand);
}
```

3. Edited `GetProduct(int id)` in `ProductsController.cs` so it makes use of specification pattern:

```c#
public async Task<ActionResult<List<Product>>> GetProduct(int id)
{
    var spec = new ProductsWithTypesAndBrandsSpecification(id);
    return Ok(await _productsRepo.GetEntityWithSpec(spec));
}
```

### 3.6. Shaping the data to return with DTO(Data Transfer Object)

1.  Create dir `Dtos` in `API`
2.  Create a class `ProductToReturnDto.cs` in `Dtos`

```c#
namespace API.Dtos
{
    public class ProductToReturnDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string PictureUrl { get; set; }
        public string ProductType { get; set; }
        public string ProductBrand { get; set; }
    }
}
```

3. Reformat `ProductsController.cs`

```c#
public async Task<ActionResult<List<ProductToReturnDto>>> GetProducts()
{
    // Create specification using ProductWithTypesAndBrandsSpec
    var spec = new ProductsWithTypesAndBrandsSpecification();
    // Use spec variable as argument in .ListAsync method
    var products = await _productsRepo.ListAsync(spec);
    return products.Select(product => new ProductToReturnDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            PictureUrl = product.PictureUrl,
            Price = product.Price,
            ProductBrand = product.ProductBrand.Name,
            ProductType = product.ProductType.Name
        }).ToList();
    }
```

```c#
public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id)
{
    var spec = new ProductsWithTypesAndBrandsSpecification(id);
    var product = await _productsRepo.GetEntityWithSpec(spec);
    return new ProductToReturnDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            PictureUrl = product.PictureUrl,
            Price = product.Price,
            ProductBrand = product.ProductBrand.Name,
            ProductType = product.ProductType.Name
        };
}
```

### 3.6.1. Adding AutoMapper to the project

1.  Install `AutoMapper.Extensions.Microsoft.DependencyInjection` NuGet package.
2.  Create a Helpers folder in `API` directory.
3.  In `Helpers` folder create `MappingProfiles.cs` file

```c#
using API.Dtos;
using AutoMapper;
using Core.Entities;

namespace API.Helpers
{
    public class MappingProfiles:Profile
    {
        public MappingProfiles()
        {
            CreateMap<Product, ProductToReturnDto>();
        }
    }
}
```

4.  Add AutoMapper as a service in `Startup.cs`

```c#
services.AddAutoMapper(typeof(MappingProfiles));
```

5.  Add `IMapper` to `ProductsController` constructor:

```c#
public ProductsController(IGenericRepository<Product> productsRepo,
    IGenericRepository<ProductBrand> productBrandRepo, IGenericRepository<ProductType> productTypeRepo, IMapper mapper)
{
    _productsRepo = productsRepo;
    _productBrandRepo = productBrandRepo;
    _productTypeRepo = productTypeRepo;
    _mapper = mapper;
}
```

6.  Refactor `GetProduct(int id)` & `GetProducts()` methods in Products Controller

```c#
public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id)
{
    var spec = new ProductsWithTypesAndBrandsSpecification(id);
    var product = await _productsRepo.GetEntityWithSpec(spec);
    return _mapper.Map<Product, ProductToReturnDto>(product);
}
```

7.  Result after calling the method from the url `https://localhost:5001/api/products/2`:

```json
{
	"id": 2,
	"name": "Core Purple Boots",
	"description": "Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Proin pharetra nonummy pede. Mauris et orci.",
	"price": 199.99,
	"pictureUrl": "images/products/boot-core1.png",
	"productType": "Core.Entities.ProductType",
	"productBrand": "Core.Entities.ProductBrand"
}
```

Note we can't see the ProductType and productBrand properties.

### 3.6.2. Configuring AutoMapper profile

1. Refactor code in `MappingProfiles.cs` constructor

```c#
public MappingProfiles()
{
    CreateMap<Product, ProductToReturnDto>()
        .ForMember(d => d.ProductBrand, o => o.MapFrom(s => s.ProductBrand.Name))
        .ForMember(d => d.ProductType, o => o.MapFrom(s => s.ProductType.Name));

}
```

This time after sending an HTTP request we get:

```json
{
	"id": 2,
	"name": "Core Purple Boots",
	"description": "Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Proin pharetra nonummy pede. Mauris et orci.",
	"price": 199.99,
	"pictureUrl": "images/products/boot-core1.png",
	"productType": "Boots",
	"productBrand": "NetCore"
}
```

2. Refactor `GetProducts()` in `ProductsController.cs`:

```c#
public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetProducts()
{
    // Create specification using ProductWithTypesAndBrandsSpec
    var spec = new ProductsWithTypesAndBrandsSpecification();
    // Use spec variable as argument in .ListAsync method
    var products = await _productsRepo.ListAsync(spec);
    //this time using auto maper to return list of productDto objects
    return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products));
}
```

After the HTTP request to https://localhost:5001/api/products/ we get:

```json
[
	{
		"id": 1,
		"name": "Typescript Entry Board",
		"description": "Aenean nec lorem. In porttitor. Donec laoreet nonummy augue.",
		"price": 120,
		"pictureUrl": "images/products/sb-ts1.png",
		"productType": "Boards",
		"productBrand": "Typescript"
	},
	{
		"id": 2,
		"name": "Core Purple Boots",
		"description": "Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Proin pharetra nonummy pede. Mauris et orci.",
		"price": 199.99,
		"pictureUrl": "images/products/boot-core1.png",
		"productType": "Boots",
		"productBrand": "NetCore"
	},
	{
		"id": 3,
		"name": "Core Red Boots",
		"description": "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Maecenas porttitor congue massa. Fusce posuere, magna sed pulvinar ultricies, purus lectus malesuada libero, sit amet commodo magna eros quis urna.",
		"price": 189.99,
		"pictureUrl": "images/products/boot-core2.png",
		"productType": "Boots",
		"productBrand": "NetCore"
	}
]
```

### 3.6.3. Adding a Custom Value Resolver for AutoMapper

1.  Add `"ApiUrl": "https://localhost:5001/"` in `appsettings.Development.json`
2.  Create `ProductUrlResolver.cs` class in `Helpers`

```c#
using API.Dtos;
using AutoMapper;
using Core.Entities;
using Microsoft.Extensions.Configuration;

namespace API.Helpers
{
    public class ProductUrlResolver:IValueResolver<Product,ProductToReturnDto,string>
    {
        private readonly IConfiguration _config;

        public ProductUrlResolver(IConfiguration config)
        {
            _config = config;
        }

        public string Resolve(Product source, ProductToReturnDto destination, string destMember, ResolutionContext context)
        {
            if (!string.IsNullOrEmpty(source.PictureUrl))
            {
                return _config["ApiUrl"] + source.PictureUrl;
            }

            return null;
        }
    }
}
```

2. Edit `MappingProfiles.cs` constructor:

```c#
        public MappingProfiles()
        {
            CreateMap<Product, ProductToReturnDto>()
                .ForMember(d => d.ProductBrand, o => o.MapFrom(s => s.ProductBrand.Name))
                .ForMember(d => d.ProductType, o => o.MapFrom(s => s.ProductType.Name))
                //add ProductUrlResolver to the mapper
                .ForMember(d => d.PictureUrl, o => o.MapFrom<ProductUrlResolver>());
        }
    }
```

### 3.6.4. Serving static content from the API

1.  Create new folder `wwwroot` in `API` directory
2.  Copy `images` folder to `wwwroot` directory
3.  In `Startup.cs` class inside `Configure(IApplicationBuilder app, IWebHostEnviroment env)` method add following line:

```c#
            app.UseRouting();
//----------------------------------------
            app.UseStaticFiles();
//----------------------------------------
            app.UseAuthorization();
```

Now we can show images from directly by clicking on the url from json object.

## 4. API Error Handling

### 4.1. Creating test controller for errors

1.  Create `BaseApiController.cs` class in `Controllers`

```c#
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController:ControllerBase
    {
    }
}
```

2.  Create new `BuggyController.cs` controller in `Controllers`

```c#
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BuggyController : BaseApiController
    {
        private readonly StoreContext _context;

        public BuggyController(StoreContext context)
        {
            _context = context;
        }

        [HttpGet("notfound")]
        public ActionResult GetNotFoundRequest()
        {
            var thing = _context.Products.Find(42);
            if (thing == null)
            {
                return NotFound();
            }
            return Ok();
        }

        [HttpGet("servererror")]
        public ActionResult GetServerError()
        {
            var thing = _context.Products.Find(42);
            var thingToReturn = thing.ToString();
            return Ok();
        }

        [HttpGet("badrequest")]
        public ActionResult GetBadRequest()
        {
            return BadRequest();
        }

        [HttpGet("badrequest/{id}")]
        public ActionResult GetNotFoundRequest(int id)
        {
            return Ok();
        }
    }
}
```

### 4.1. Create a consistent error response from the API

1.  Inside `API` create folder `Errors`
2.  In `Errors` folder create a new class `ApiResponse`

```c#
namespace API.Errors
{
    public class ApiResponse
    {
        public ApiResponse(int statusCode, string message=null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        }
        public int StatusCode { get; set; }
        public string Message { get; set; }
        private string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "You have made a bad request",
                401 => "You are not authorized",
                404 => "Resource not found",
                500 => "Server Error",
                _ => null
            };
        }
    }
}
```

3. Include ApiResponse class inside return statements in `BuggyController.cs` class

```c#
[HttpGet("badrequest")]
        public ActionResult GetBadRequest()
        {
            return BadRequest(new ApiResponse(400));
        }
```

```c#
        [HttpGet("notfound")]
        public ActionResult GetNotFoundRequest()
        {
            var thing = _context.Products.Find(42);
            if (thing == null)
            {
                return NotFound(new ApiResponse(404));
            }
            return Ok();
        }
```

4.  In `Controllers` create new `ErrorController.cs` class

```c#
using API.Errors;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Microsoft.AspNetCore.Components.Route("errors/{code}")]
    public class ErrorController : BaseApiController
    {
        public IActionResult Error(int code)
        {
            return new ObjectResult(new ApiResponse(code));
        }
    }
}
```

5.  Added following line in `Configure()` method inside`Startup.cs` class

```c#
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            //Added following line
            app.UseStatusCodePagesWithReExecute("/errors/{0}");
            //-------------------------------------------------
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
```

6.  Now if we hit endpoint that doesn't exist from the browser eg. https://localhost:5001/api/endpointthatdoesnotexist we get a response:

```json
{
	"statusCode": 404,
	"message": "Resource not found"
}
```

### 4.2. Creating Exception handler middleware

1.  In `Errors` folder create a new class `ApiException`

```c#
namespace API.Errors
{
   public class ApiException:ApiResponse
   {
       public ApiException(int statusCode, string message = null,string details=null) : base(statusCode, message)
       {
           Details = details;
       }

       public string Details { get; set; }
   }
}
```

2.  Inside `API` folder create new folder `Middleware`
3.  Inside `Middleware` create `ExceptionMiddleware.cs` class

```c#
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using API.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next,ILogger<ExceptionMiddleware> logger,IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

                var response = _env.IsDevelopment()
                    ? new ApiException((int) HttpStatusCode.InternalServerError, ex.Message, ex.StackTrace.ToString())
                    : new ApiException((int) HttpStatusCode.InternalServerError);

                var options = new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
                var json = JsonSerializer.Serialize(response,options);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
```

3.  Add `ExceptionMiddleware` in `Startup` config:

We replace:

```c#
if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
```

with:

```c#
app.UseMiddleware<ExceptionMiddleware>();
```

4.  Postman response with the https://localhost:5001/api/buggy/servererror url:

```json
{
	"Details": "   at API.Controllers.BuggyController.GetServerError() in C:\\Users\\korisnik\\Desktop\\dev\\ecommerce-shop\\API\\Controllers\\BuggyController.cs:line 31\r\n   at lambda_method12(Closure , Object , Object[] )\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.SyncActionResultExecutor.Execute(IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeActionMethodAsync()\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeNextActionFilterAsync()\r\n--- End of stack trace from previous location ---\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeInnerFilterAsync()\r\n--- End of stack trace from previous location ---\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeFilterPipelineAsync>g__Awaited|19_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\r\n   at Microsoft.AspNetCore.Routing.EndpointMiddleware.<Invoke>g__AwaitRequestTask|6_0(Endpoint endpoint, Task requestTask, ILogger logger)\r\n   at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)\r\n   at Microsoft.AspNetCore.Diagnostics.StatusCodePagesMiddleware.Invoke(HttpContext context)\r\n   at API.Middleware.ExceptionMiddleware.InvokeAsync(HttpContext context) in C:\\Users\\korisnik\\Desktop\\dev\\ecommerce-shop\\API\\Middleware\\ExceptionMiddleware.cs:line 29",
	"StatusCode": 500,
	"Message": "Object reference not set to an instance of an object."
}
```

### 4.3. Improving validation error responses

1.  Inside `Errors` create a new class `ApiValidationErrorResponse.cs`

```c#
using System.Collections.Generic;

namespace API.Errors
{
    public class ApiValidationErrorResponose : ApiResponse
    {
        public ApiValidationErrorResponose() : base(400)
        {
        }

        public IEnumerable<string> Errors { get; set; }
    }
}
```

2.  In `Startup.cs` at the end of `public void ConfigureServices(IServiceCollection services)` add following lines:

```c#
services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var errors = actionContext.ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage).ToArray();

                    var errorResponse = new ApiValidationErrorResponose
                    {
                        Errors = errors
                    };
                    return new BadRequestObjectResult(errorResponse);
                };
            });
```

### 4.4. Adding Swagger for documenting API

1.  Install required packages if not installed already. See `API.sln` for more info.
2.  Add following lines in `Configure` and `ConfigureServices` methods in `Startup.cs` <br><br>

At the end of `ConfigureServices()` add line:

```c#
services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "API", Version = "v1"}); });
```

In `Configure()` add:

```c#
app.UseMiddleware<ExceptionMiddleware>();
//After the line above add following lines - the ordering is important
app.UseSwagger();

app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "API V1"); });
```

3.  Added following line in `ErrorController.cs`

```c#
    [Route("errors/{code}")]
 //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
    [ApiExplorerSettings(IgnoreApi=true)]
//-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
    public class ErrorController : BaseApiController{...}
```

4.  Added following line in `GetProduct(int id)` method inside `ProductsController` so it returns an error page if we enter non-existing id of the product:

```c#
if (product == null) return NotFound(new ApiResponse(404));
```

5. We can add `ProduceResponseType` before property before for example `GetProduct(int id)` method to be able to see response types in swagger

```c#
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse),StatusCodes.Status404NotFound)]
public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id){...}
```

<<<<<<< HEAD

### 5. API Paging,Filtering,Sorting & Searching

#### 5.1. Adding sorting specification class

1. Added new methods in `ISpecification.cs`

```c#
Expression<Func<T,object>> OrderBy { get; }
Expression<Func<T,object>> OrderByDescending { get; }
```

2. Implemented following methods in `BaseSpecification.cs`

```c#
public Expression<Func<T, object>> OrderBy { get; private set; }
public Expression<Func<T, object>> OrderByDescending { get; private set; }
//order by expression
protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
{
    OrderBy = orderByExpression;
}
//order by descending
protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
{
    OrderByDescending = orderByDescExpression;
}
```

3. Added new specification methods to query inside `getQuery()` method in `SpecificationEvaluator.cs` class:

```c#
//add Criteria to query expression
//init
if (spec.Criteria != null)
{
    query = query.Where(spec.Criteria); // ex. p=>p.ProductTypeId==id
}
//1. add OrderBy to query expression
if (spec.OrderBy != null)
{
    query = query.OrderBy(spec.OrderBy);
}
//2. add OrderByDesc to query expression
if (spec.OrderByDescending != null)
{
    query = query.OrderByDescending(spec.OrderByDescending);
}
```

4. Updated `ProductsWithTypesAndBrandsSpecification` constructor to now take string `sort` as argument:

```c#
//defualt mode=sort by name
 AddOrderBy(x=>x.Name);
//according to sort prop
if (!string.IsNullOrEmpty(sort))
{
    //sort by:
    switch (sort)
    {
        // ascending price
        case "priceAsc":
            AddOrderBy(p=>p.Price);
            break;
        // descending price
        case "priceDesc":
            AddOrderByDescending(p=>p.Price);
            break;
        default:
            // by name,default
            AddOrderBy(n=>n.Name);
            break;
    }
```

5. In `Controllers` refactored `GetProducts(string sort)`

```c#
//now take string as argument(type of sorting that will be executed)
public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetProducts(string sort)
{
    // Create specification using ProductWithTypesAndBrandsSpec
    var spec = new ProductsWithTypesAndBrandsSpecification(sort);
    var products = await _productsRepo.ListAsync(spec);
    return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products));
}
```

6.  Now if we send a request to our localhost https://localhost:5001/api/products?sort=priceAsc we get an exception:

```json
{
	"details": "...",
	"statusCode": 500,
	"message": "SQLite cannot order by expressions of type 'decimal'. Convert the values to a supported type or use LINQ to Objects to order the results."
}
```

#### 5.2. Solving the decimal problem with SQlite

1.  Refactor `OnModelCreating(ModelBuilder modelBuilder)` method inside `StoreContext.cs` class

```c#
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    // check if API is using Sqlite database
    if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            //get all decimal values from database
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(decimal));

            foreach (var property in properties)
            {
                // dealing with decimal prop
                modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();
            }
        }
    }
}
```

#### 5.3. Adding filtering functionality

1.  Modified `GetProducts()` method so it now takes two more optional arguments `typeId` & `brandId`

```c#
public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetProducts(
            string sort,int? brandId,int? typeId)
```

2. Modified public `ProductsWithTypesAndBrandsSpecification.cs` class constructor

```c#
 public ProductsWithTypesAndBrandsSpecification(string sort,int? brandId,int? typeId)
  //added following:
            : base(x=>(!brandId.HasValue || x.ProductBrandId == brandId) &&
                   (!typeId.HasValue || x.ProductTypeId==typeId))
```

#### 5.4. Adding Pagination

1.  Added properties in `ISpecification.cs` interface

```c#
// We need following properties for our pagination mechanism
// Take number of products
int Take { get; }
// Skip number of products
int Skip { get; }
// Pagination on/off
bool IsPagingEnabled { get; }
```

2.  Implemented new properties and added `ApplyPaging(int,int)` method

```c#
public int Take { get; private set; }
public int Skip { get; private set; }
public bool IsPagingEnabled { get; private set; }
protected void ApplyPaging(int skip, int take)
{
    Skip = skip;
    Take = take;
    IsPagingEnabled = true;
}
```

3.  Configured `GetQuery()` method in `SpecificEvaluator` class

```c#
 // added following lines
 // add paging
if (spec.IsPagingEnabled)
{
    query = query.Skip(spec.Skip).Take(spec.Take);
}
```

4.  To clean up `GetProducts(string,int,int)` method and add constraints to page mechanism properties,<br>new class `ProductSpecParams.cs` in `..\Core\Specifications\` is created:

```c#
namespace API.Specifications
{
    public class ProductSpecParams
    {
        private const int MaxPageSize = 50;
        public int PageIndex { get; set; } = 1;
        private int _pageSize = 6;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
        public int? BrandId { get; set; }
        public int? TypeId { get; set; }
        public string Sort { get; set; }
    }
}
```

5.  Now we can call `ProductSpecParams` object instead of arguments

```c#
public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetProducts([FromQuery]ProductSpecParams productParams)
        {
            var spec = new ProductsWithTypesAndBrandsSpecification(productParams);
            ...
         }
```

6.  Refactor `ProductWithTypesAndBrandSpecification.cs`(fix errors)
7.  Added paging to `ProductsWithTypesAndBrandsSpecification` constructor

```c#
AddInclude(x => x.ProductType);
AddInclude(x => x.ProductBrand);
AddOrderBy(x => x.Name);
// Added paging
ApplyPaging(productParams.PageSize*(productParams.PageIndex-1),productParams.PageSize);
```

8.  Inside `Helpers` folder in `API` create a class `Pagination.cs`

```c#
using System.Collections.Generic;

namespace API.Helpers
{
    public class Pagination<T> where T:class
    {
        public Pagination(int pageIndex, int pageSize, int count, IReadOnlyList<T> data)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            Count = count;
            Data = data;
        }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public IReadOnlyList<T> Data { get; set; }
    }
}
```

9.  In IGenericRepository added following method:

```c#
        Task<int> CountAsync(ISpecification<T> spec);
```

10. Implemented method in `GenericRepository.cs`

```c#
public async Task<int> CountAsync(ISpecification<T> spec)
{
    return await ApplySpecification(spec).CountAsync();
}
```

11. In `Specification` dir create another new class `ProductWithFiltersCountSpecification.cs`

```c#
using Core.Entities;

namespace API.Specifications
{
    public class ProductWithFiltersForCountSpecification:BaseSpecification<Product>
    {
        public ProductWithFiltersForCountSpecification(ProductSpecParams productParams): base(x => (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
            (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId))
        {

        }
    }
}
```

12. Modify `ProductController.cs`

```c#
        public async Task<ActionResult<Pagination<ProductToReturnDto>>> GetProducts([FromQuery]ProductSpecParams productParams)
        {
            // Now takes brandID & TypeId properties;
            var spec = new ProductsWithTypesAndBrandsSpecification(productParams);
            var countSpec = new ProductWithFiltersForCountSpecification(productParams);
            var totalItems = await _productsRepo.CountAsync(spec);
            var products = await _productsRepo.ListAsync(spec);
            var data= _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products);
            return Ok(new Pagination<ProductToReturnDto>(productParams.PageIndex, productParams.PageSize, totalItems,
                data));
        }
```

### 5.5 Adding the Search functionality

1.  Added following \_search field in `ProductSpecParams.cs`

```c#
private string _search;

        public string Search
        {
            get=>_search;
            set=>_search=value.ToLower();
        }
```

2.  Refactored the `ProductWithTypesAndBrandsConstructor`

```c#
public ProductsWithTypesAndBrandsSpecification(ProductSpecParams productParams)
            : base(x =>
                        (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search))&&
                        (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
                        (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId))
```

3.  And also `ProductWithFiltersForCountSpecification`

```c#
        public ProductWithFiltersForCountSpecification(ProductSpecParams productParams)
            :base(x =>
        (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search))&&
        (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
        (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId))
```

### 5.6. Adding CORS Support to the API

1.  In `ConfigureServices()` method inside `Startup.cs` add a new service

```c#
services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().WithOrigins("https://localhost:4200");
    });
});
```

& to the `Configure()` method also

```c#
app.UseCors("CorsPolicy");
```

### 5.7. Cleaning up `Startup.cs` file

1.  Create new directory `Extensions` in `API`
2.  Add new class `ApplicationServicesExtensions` in `Extensions` directory

```c#
using System.Linq;
using API.Errors;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace API.Extension
{
    public static class ApplicationServicesExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var errors = actionContext.ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage).ToArray();

                    var errorResponse = new ApiValidationErrorResponose
                    {
                        Errors = errors
                    };
                    return new BadRequestObjectResult(errorResponse);
                };
            });
            return services;
        }
    }
}
```

3.  Inside `Extensions` create new class `SwaggerServicesExtensions`

```c#
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace API.Extension
{
    public static class SwaggerServiceExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "API", Version = "v1"}); });
            return services;
        }

        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "API V1"); });
            return app;
        }
    }

}
```

4.  In `Startup.cs` delete lines copied to files above and use created method instead so now we cleaner `Startup.cs` file:

```c#
using API.Extension;
using API.Helpers;
using API.Middleware;
using Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingProfiles));
            services.AddControllers();
            services.AddDbContext<StoreContext>(x =>
                x.UseSqlite(_config.GetConnectionString("DefaultConnection")));
            services.AddApplicationServices();
            services.AddSwaggerDocumentation();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseSwaggerDocumentation();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
```

### 6. API - Basket

#### 6.1. Setting up Redis

1.  Install `StackExchange.Redis` via NuGet package manager in Infrastructure project.
2.  Add it as a service in `ConfigurationServices(IServiceCollection services)` in `Startup.cs` class

```c#
  services.AddSingleton<IConnectionMultiplexer>(c =>
            {
                var configuration = ConfigurationOptions.Parse(_config.GetConnectionString("Redis"),
                    true);
                return ConnectionMultiplexer.Connect(configuration);
            });
```

3.  In `appsettings.Development.json` add a connection string

```c#
 "ConnectionStrings": {
    "DefaultConnection": "Data source=e-commerce.db",
     "Redis": "localhost" //added connection string to use it locally
  }
```

#### 6.2. Setting up a basket class

1.  In `Core/Entities` create a new class `BasketItem.cs`

```c#
namespace Core.Entities
{
    public class BasketItem
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string PictureUrl { get; set; }
        public string Brand { get; set; }
        public string Type { get; set; }
    }
}
```

2.  In same directory create another class `CustomerBasket.cs`

```c#
using System.Collections.Generic;

namespace Core.Entities
{
    public class CustomerBasket
    {
        public CustomerBasket(string id)
        {
            Id = id;
        }

        public CustomerBasket()
        {
        }

        public string Id { get; set; }
        public List<BasketItem> Items { get; set; } = new List<BasketItem>();

    }
}
```

#### 6.3. Creating a basket repository interface

1.  Inside `Core/Interfaces` created new interface `IBasketRepository.cs`

```c#
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IBasketRepository
    {
        Task<CustomerBasket> GetBasketAsync(string basketId);
        Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
        Task<bool> DeleteBasketAsync(string basketId);
    }
}
```

2.  Inside `Infrastructure/Data` created new class `BasketRepository.cs`
3.  Inside `ApplicationServicesExtensions.cs` add it as a service

```c#
 services.AddScoped<IBasketRepository, BasketRepository>();
```

#### 6.4. Implementing the basket repository

```c#
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Data
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IDatabase _database;
        public BasketRepository(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task<CustomerBasket> GetBasketAsync(string basketId)
        {
            var data = await _database.StringGetAsync(basketId);
            return data.IsNullOrEmpty ? null : JsonSerializer.Deserialize<CustomerBasket>(data);
        }

        //update or create a basket
        public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
        {
            var created = await _database.StringSetAsync(basket.Id, JsonSerializer.Serialize(basket), TimeSpan.FromDays(30));

            if (!created) return null;

            return await GetBasketAsync(basket.Id);
        }

        public async Task<bool> DeleteBasketAsync(string basketId)
        {
            return await _database.KeyDeleteAsync(basketId);
        }
    }
}
```

### 6.5. Adding a basket controller

1.  In `API/Controllers` create a new c# class `BasketController.cs`

```c#
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BasketController : BaseApiController
    {
        private readonly IBasketRepository _basketRepository;

        public BasketController(IBasketRepository basketRepository)
        {
            _basketRepository = basketRepository;
        }

        [HttpGet]
        public async Task<ActionResult<CustomerBasket>> GetBasketById(string id)
        {
            var basket = await _basketRepository.GetBasketAsync(id);
            return Ok(basket ?? new CustomerBasket(id));
        }

        [HttpPost]
        public async Task<ActionResult<CustomerBasket>> UpdateBasket(CustomerBasket basket)
        {
            var updatedBasket = await _basketRepository.UpdateBasketAsync(basket);
            return Ok(updatedBasket);
        }

        [HttpDelete]
        public async Task DeleteBasketAsync(string id)
        {
            await _basketRepository.DeleteBasketAsync(id);
        }
    }
}
```

### 6.6. Installing Redis with Docker

1.  Install docker on a development machine
2.  Add `docker-compose.yml` in main project directory
3.  Running the docker image

```
docker-compose up --detach
```

### 7. Identity

#### 7.1. Setting up identity packages

1.  In Core project install `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package via NuGet
2.  In Infrastructure project install `Microsoft.AspNetCore.Identity`, `Microsoft.IdentityModel.Tokens` and `System.IdentityModel.Tokens.Jwt` packages via NuGet

#### 7.2. Setting up the identity classes

1.  In `Core/Entities` create a new directory `Identity`
2.  Inside `Identity` folder create a new class `AppUser.cs`

```c#
using System.Net.Sockets;
using Microsoft.AspNetCore.Identity;

namespace Core.Entities.Identity
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public Address Address { get; set; }
    }
}
```

3. In `Identity` folder create a class `Address.cs`

```c#
using System.ComponentModel.DataAnnotations;

namespace Core.Entities.Identity
{
    public class Address
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }

        [Required]
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}
```

### 7.3. Adding the IdentityDbContext

1.  In Infrastructure folder create a new directory `Identity`
2.  Inside new created directory create a class `AppIdentityDbContext.cs`

```c#
using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity
{
    public class AppIdentityDbContext : IdentityDbContext<AppUser>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
```

3.  Add this as a service in `Startup.cs`

```c#
services.AddDbContext<AppIdentityDbContext>(x =>
{
    x.UseSqlite(_config.GetConnectionString("IdentityConnection"));
});
```

4.  In `appsettings.Development.json` add a connection string

```json
 "ConnectionStrings": {
    "DefaultConnection": "Data source=e-commerce.db",
    "IdentityConnection": "Data source=identity.db", // <=
     "Redis": "localhost"
  }
```

### 7.4 Adding a new migration

1.  From terminal

```
dotnet ef migrations add IdentityInitial -p Infrastructure -s API -c AppIdentityDbContext -o Identity/Migrations
```

2.  Inside `Infrastructure/Identity` create a new class `AppIdentityDbContextSeed.cs`

```c#
using System.Linq;
using System.Threading.Tasks;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public class AppIdentityDbContextSeed
    {
        public static async Task SeedUserAsync(UserManager<AppUser> userManager)
        {
            if (!userManager.Users.Any())
            {
                var user = new AppUser
                {
                    DisplayName = "Bob",
                    Email = "bob@test.com",
                    UserName = "bob@test.com",
                    Address = new Address
                    {
                        FirstName = "Bob",
                        LastName = "Bobbity",
                        Street = "10 The Street",
                        City = "New York",
                        State = "NY",
                        ZipCode = "90210"
                    }
                };

                await userManager.CreateAsync(user, "Pa$$w0rd");
            }
        }
    }
}
```

#### 7.5. Adding the Startup services for identity

1.  Inside `API/Extensions` create new class `IdentityServiceExtensions.cs`

```c#
using Core.Entities.Identity;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace API.Extension
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            var builder = services.AddIdentityCore<AppUser>();
            builder = new IdentityBuilder(builder.UserType, builder.Services);
            builder.AddEntityFrameworkStores<AppIdentityDbContext>();
            builder.AddSignInManager<SignInManager<AppUser>>();

            services.AddAuthentication();
            return services;
        }
    }
}
```

2.  Add this to `Startup.cs`

```c#
services.AddIdentityServices();
```

#### 7.6. Adding identity program class

1.  Little configuring in `Program.cs`

```c#
try
{
    var context = services.GetRequiredService<StoreContext>();
    await context.Database.MigrateAsync();
    await StoreContextSeed.SeedAsync(context,loggerFactory);
    // identity config start
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var identityContext = services.GetRequiredService<AppIdentityDbContext>();
    await identityContext.Database.MigrateAsync();
    await AppIdentityDbContextSeed.SeedUserAsync(userManager);
    //identity config end
}
```

#### 7.7. Adding an Account controller

1.  Inside `API/Dtos` create `UserDto.cs`

```c#
namespace API.Dtos
{
    public class UserDto
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Token { get; set; }
    }
}
```

2.  Inside `API/Dtos` create `LoginDto.cs`

```c#
namespace API.Dtos
{
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
```

3.  Inside `API/Controllers` create a new class `AccountController.cs`

```c#
using System.Threading.Tasks;
using API.Dtos;
using API.Errors;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class AccountController: BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null) return Unauthorized(new ApiResponse(401));

            var result = await _signInManager.CheckPasswordSignInAsync(user,loginDto.Password,false);

            if (!result.Succeeded) return Unauthorized(new ApiResponse(401));

            return new UserDto
            {
                Email = user.Email,
                Token = "This will be a token",
                DisplayName = user.DisplayName
            };
        }
    }
}
```

### 7.8 Registering a user

1.  In `Dtos` create new class `RegisterDto.cs`

```c#
namespace API.Dtos
{
    public class RegisterDto
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
```

2 . In `AccountController.cs` added following method

```c#
[HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        var user = new AppUser
        {
            DisplayName = registerDto.DisplayName,
            Email = registerDto.Email,
            UserName = registerDto.Email
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded) return BadRequest(new ApiResponse(400));

        return new UserDto
        {
            DisplayName = user.DisplayName,
            Token = "This will be a toke",
            Email = user.Email
        };
    }
```

### 7.9. Adding a token generation service

1.  Inside `Core/Interfaces` create a new interface `ITokenService.cs`

```c#
using Core.Entities.Identity;

namespace Core.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(AppUser user);
    }
}
```

2.  Inside `Infrastructure` create a new folder `Services`
3.  Inside `Services` directory make a new class `TokenService.cs`

```c#
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Entities.Identity;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;

        public TokenService(IConfiguration config)
        {
            _config = config;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Token:Key"]));
        }

        public string CreateToken(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.DisplayName)
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds,
                Issuer = _config["Token:Issuer"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
```

4.  In `IdentityServiceExtensions.cs` configure the `services.AddAuthentiocation` method and edited constructor parameters

```c#
// Constructor now takes IConfig config as argument too
public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
```

```c#
// authentication configuration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Token:Key"])),
            ValidIssuer = config["Token:Issuer"],
            ValidateIssuer = true,
        };
    });
```

5.  In `Startup.cs` we now need to pass the config to AddIdentityServices method

```c#
services.AddIdentityServices(_config);
```

6.  In `appsettings.Development.json` below `"ConnectionStrings:{}"` add a Token field

```json
  "Token": {
    "Key": "super secret key",
    "Issuer": "https://localhost:5001"
  }
```

7.  In `Startup.cs` just above the `app.UseAuthorization()` add Authentication:

```c#
            app.UseAuthentication();

            app.UseAuthorization(); // <=
```

#### 7.10. Testing the token

1.  In `ApplicationServiceExtensions.cs` add `TokenService`

```c#
services.AddScoped<ITokenService, ITokenService>();
```

2.  In `AccountController.cs` refactored the constructor

```c#
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }
```

Now we can use our token service to generate tokens when we return `UserDto` objects

```c#
return new UserDto
{
    Email = user.Email,
    Token = _tokenService.CreateToken(user),
    DisplayName = user.DisplayName
};
```

```c#
return new UserDto
{
    DisplayName = user.DisplayName,
    Token = _tokenService.CreateToken(user),
    Email = user.Email
};
```

3. In `BuggyController.cs` added following method

```c#
[HttpGet("testauth")]
[Authorize]
public ActionResult<string> GetSecretText()
{
    return "secret stuff";
        }
```

#### 7.11. Troubleshooting authorization issues

1.  After logging the requests sent via Postman its is clear that the problem was with Audience Validation method inside JwtBeaver. We can override configuration parameter to resolve this.

```c#
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Token:Key"])),
            ValidIssuer = config["Token:Issuer"],
            ValidateIssuer = true,
            ValidateAudience = false, //turn audience validation off
        };
    });
```

### 7.12. Adding additional account methods to be able to read user address as well(right now returns null)

1.  In `Extensions` directory add `UserManagerExtensions`

```c#
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Extension
{
    public static class UserManagerExtensions
    {
        public static async Task<AppUser> FindByEmailWithAddressAsync(this UserManager<AppUser> input, ClaimsPrincipal user)
        {
            var email = user.FindFirstValue(ClaimTypes.Email);
            return await input.Users.Include(x => x.Address).SingleOrDefaultAsync(x => x.Email == email);
        }

        public static async Task<AppUser> FindEmailFromClaimsPrinciple(this UserManager<AppUser> input,
            ClaimsPrincipal user)
        {
            var email = user?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            return await input.Users.SingleOrDefaultAsync(x => x.Email == email);
        }
    }
}

```

2.  Refactor methods in `AccountController.cs`

```c#
[Authorize]
[HttpGet]
public async Task<ActionResult<UserDto>> GetCurrentUser()
{
    var user = await _userManager.FindByEmailFromClaimsPrinciple(User);

    return new UserDto
    {
        Email = user.Email,
        Token = _tokenService.CreateToken(user),
        DisplayName = user.DisplayName
    };
}
```

```C#
[Authorize]
[HttpGet("address")]
public async Task<ActionResult<Address>> GetUserAddress()
{

    var user = await _userManager.FindUserByClaimsPrincipleWithAddressAsync(User);

    return user.Address;
        }
```

3.  In `API/Dtos` create a new class `AdressDto.cs`

```c#
namespace API.Dtos
{
    public class AddressDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
    }
}
```

4.  In `API\Helpers` folder inside `MappingProfiles.cs` create a new mapping profile

```c#
CreateMap<Address, AddressDto>().ReverseMap(); //.ReversMap() meeans it can work in reverse too
```

5.  Now bring IMapper as argument in the constructor

```c#
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService,IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _mapper = mapper;
        }
```

5.  Refactoring `GetUserAddress()` method again

```c#
        [Authorize]
        [HttpGet("address")]
        public async Task<ActionResult<AddressDto>> GetUserAddress()
        {

            var user = await _userManager.FindUserByClaimsPrincipleWithAddressAsync(User);

            return _mapper.Map<Address, AddressDto>(user.Address);
        }
```

6.  Add a new method `UpdateUserAddress()`

```c#
[Authorize]
[HttpPut("address")]
public async Task<ActionResult<AddressDto>> UpdateUserAddress(AddressDto address)
{
    var user = await _userManager.FindUserByClaimsPrincipleWithAddressAsync(User);
    user.Address = _mapper.Map<AddressDto, Address>(address);

    var result = await _userManager.UpdateAsync(user);

    if (result.Succeeded) return Ok(_mapper.Map<Address, AddressDto>(user.Address));

    return BadRequest("Problem updating then user");

}
```

### 8. API - Validation

#### 8.1. Model validation

1.  To set the constraint to the address object so it does not accept null values we go to `AddressDto.cs` and set `[Required]` property on all properties

```c#
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class AddressDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Street { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string ZipCode { get; set; }
    }
}
```

2.  Now if we send a request to update address with null values to our API we get the response:

```json
{
	"errors": [
		"The City field is required.",
		"The State field is required.",
		"The Street field is required.",
		"The ZipCode field is required."
	],
	"statusCode": 400,
	"message": "You have made a bad request"
}
```

#### 8.2. Checking for duplicate email addresses

1.  If we send a Http POST request to register a user with empty password field we get a response:

```json
{
	"statusCode": 400,
	"message": "You have made a bad request"
}
```

2.  To be able to handle error better and give more details we add constraints to `RegisterDto.cs` class as well:

```c#
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class RegisterDto
    {
        [Required]
        public string DisplayName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [RegularExpression("(?=^.{6,10}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&amp;*()_+}{&quot;:;'?/&gt;.&lt;,])(?!.*\\s).*$")]
        public string Password { get; set; }
    }
}
```

3.  Now we can handle the case where email address already exists from our `AccountController.cs`. In the beginning of `Register` method we add:

```c#
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (CheckEmailExistsAsync(registerDto.Email).Result.Value)
        {
            return new BadRequestObjectResult(new ApiValidationErrorResponose
            {
                Errors = new[] {"Email address already in use"}
            });
        }
    //... rest of the code continue executing from here
    }
```

4.  Now our error responses look like this:</br></br>For emtpy email and password fields we sent a request:

```json
{
	"firstName": "Tom",
	"lastName": "Smith"
}
```

</br>We get a response:

```json
{
	"errors": [
		"The Email field is not a valid e-mail address.",
		"The Password field is required."
	],
	"statusCode": 400,
	"message": "You have made a bad request"
}
```

</br>Bad Email Address:

```json
{
	"displayName": "D",
	"email": "notanemail",
	"password": "pa$$w0rD"
}
```

</br>We get a response:

```json
{
	"errors": ["The Email field is not a valid e-mail address."],
	"statusCode": 400,
	"message": "You have made a bad request"
}
```

</br>And for a weak password:

```json
{
	"displayName": "Q",
	"email": "notanemail@gmail.com",
	"password": "123456"
}
```

</br>We get a response:

```json
{
	"errors": [
		"Password must have 1 uppercase, 1 lowercase, 1 number, 1 non alphanumeric and at least 6 characters"
	],
	"statusCode": 400,
	"message": "You have made a bad request"
}
```

#### 8.3. Validating a basket

1.  Create a new Dto class in `API/Dtos` - `CustomerBasketDto.cs`

```c#
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class CustomerBasketDto
    {
        [Required] public string Id { get; set; }
        public List<BasketItemDto> Items { get; set; }
    }
}
```

2.  Create a `BasketItemDto.cs` in `Dtos`

```c#
using System.ComponentModel.DataAnnotations;

namespace API.Dtos
{
    public class BasketItemDto
    {
        [Required] public int Id { get; set; }
        [Required] public string ProductName { get; set; }

        [Required]
        [Range(0.1, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required] public string PictureUrl { get; set; }
        [Required] public string Brand { get; set; }
        [Required] public string Type { get; set; }
    }
}
}
```

3.  In mapping profiles we add two additional mapping profiles:

```c#
CreateMap<CustomerBasketDto, CustomerBasket>();
CreateMap<BasketItemDto, BasketItem>();
```

4.  Refactor `BasketController.cs` </br> Inject IMapper

```c#
        private readonly IBasketRepository _basketRepository;
        private readonly IMapper _mapper; // initialzed field from the parametr

        public BasketController(IBasketRepository basketRepository, IMapper mapper)
        {
            _basketRepository = basketRepository;
            _mapper = mapper;
        }
```

Refactor `UpdateBasket` method:

```c#
        [HttpPost]
        public async Task<ActionResult<CustomerBasket>> UpdateBasket(CustomerBasketDto basket)
        {
            var customerBasket = _mapper.Map<CustomerBasketDto, CustomerBasket>(basket);
            var updatedBasket = await _basketRepository.UpdateBasketAsync(customerBasket);
            return Ok(updatedBasket);
        }
```

#### 8.4. Updating swagger config for identity

1.  We go to `SwaggerServiceExtensions.cs`

```c#
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "API", Version = "v1"});
                //add securitySchema
                var securitySchema = new OpenApiSecurityScheme
                {
                    Description = "JWT Auth Bearer Scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };
                //config
                c.AddSecurityDefinition("Bearer",securitySchema);
                var securityRequirement = new OpenApiSecurityRequirement {{securitySchema, new[] {"Bearer"}}};
                c.AddSecurityRequirement(securityRequirement);
            });
            return services;
        }
```

### 9. API - Orders

#### 9.1. Creating the order aggregate

1.  In `Core/Entities` create new dir OrderAggregate
2.  In `OrderAggregate` dir create a new class `Address.cs`

```c#
namespace Core.Entities.OrderAggregate
{
    public class Address
    {
        public Address(string firstName, string lastName, string street, string city, string state, string zipCode)
        {
            FirstName = firstName;
            LastName = lastName;
            Street = street;
            City = city;
            State = state;
            ZipCode = zipCode;
        }

        public Address()
        {
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
    }
}
```

3.  In same dir create another new class `DeliveryMethod.cs`

```c#
namespace Core.Entities.OrderAggregate
{
    public class DeliveryMethod: BaseEntity
    {
        public string ShortName { get; set; }
        public string DeliveryTime { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }
}
```

4.  In `Entities/OrderAggregate` create another class `ProductItemOrdered.cs`

```c#
namespace Core.Entities.OrderAggregate
{
    public class ProductItemOrdered
    {
        public ProductItemOrdered()
        {
        }

        public ProductItemOrdered(int productItemId, string productName, string pictureUrl)
        {
            ProductItemId = productItemId;
            ProductName = productName;
            PictureUrl = pictureUrl;
        }

        public int ProductItemId { get; set; }
        public string ProductName { get; set; }
        public string PictureUrl { get; set; }
    }
}
```

5.  Create new enum `OrderStatus.cs` in same dir

```c#
using System.Runtime.Serialization;

namespace Core.Entities.OrderAggregate
{
    public enum OrderStatus
    {
        [EnumMember(Value = "Pending")]
        Pending,
        [EnumMember(Value = "Payment Received")]
        PaymentReceived,
        [EnumMember(Value = "Payment Failed")]
        PaymentFailed
    }
}
```

6.  Create another new class `OrderItem.cs`

```c#
namespace Core.Entities.OrderAggregate
{
    public class OrderItem: BaseEntity
    {
        public OrderItem()
        {
        }

        public OrderItem(ProductItemOrdered itemOrdered, decimal price, int quantity)
        {
            ItemOrdered = itemOrdered;
            Price = price;
            Quantity = quantity;
        }

        public ProductItemOrdered ItemOrdered { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
```

7.  Create another new class `Order.cs`

```c#
using System;
using System.Collections.Generic;

namespace Core.Entities.OrderAggregate
{
    public class Order: BaseEntity
    {
        public Order(IReadOnlyList<OrderItem> orderItems,string buyerEmail, Address shipToAddress, DeliveryMethod deliveryMethod,  decimal subtotal)
        {
            BuyerEmail = buyerEmail;
            ShipToAddress = shipToAddress;
            DeliveryMethod = deliveryMethod;
            OrderItems = orderItems;
            Subtotal = subtotal;
        }

        public Order()
        {
        }

        public string BuyerEmail { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.Now;
        public Address ShipToAddress { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public IReadOnlyList<OrderItem> OrderItems { get; set; }
        public decimal Subtotal { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string PaymentId { get; set; }

        public decimal GetTotal()
        {
            return Subtotal + DeliveryMethod.Price;
        }
    }
}
```

### 9.2. Configuring the order entities

1.  In `Infrastructure/Data/Config` create a new class `OrderConfiguration.cs`

```c#
using System;
using Core.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
   public class OrderConfiguration : IEntityTypeConfiguration<Order>
   {
       public void Configure(EntityTypeBuilder<Order> builder)
       {
           builder.OwnsOne(o => o.ShipToAddress, a => { a.WithOwner(); });
           builder.Property(s => s.Status)
               .HasConversion(
                   o => o.ToString(),
                   o => (OrderStatus) Enum.Parse(typeof(OrderStatus), o));
           builder.HasMany(o => o.OrderItems).WithOne().OnDelete(DeleteBehavior.Cascade);
       }
   }
}
```

2.  In `Config` dir create another class `OrderItemConfiguration.cs`

```c#
using Core.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
    public class OrderItemConfiguration: IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.OwnsOne(i => i.ItemOrdered, io => { io.WithOwner(); });
            builder.Property(i => i.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}
```

3.  In `Config` create another new config class `DeliveryMethodConfiguration.cs`

```c#
using Core.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
    public class DeliveryMethodConfiguration : IEntityTypeConfiguration<DeliveryMethod>
    {
        public void Configure(EntityTypeBuilder<DeliveryMethod> builder)
        {
            builder.Property(d => d.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}
```

#### 9.3. Updating store context and seeding delivery methods

1.  Add new members to `StoreContext.cs` class

```c#
public DbSet<Order> Orders { get; set; }
public DbSet<OrderItem> OrderItems { get; set; }
public DbSet<DeliveryMethod> DeliveryMethods { get; set; }

```

2.  In `StoreContextSeed.cs` updated `SeedAsync()` method to new seed data from `delivery.json` file too

```c#
// seed delivery.json data if not present in data context already
if (!context.DeliveryMethods.Any())
{
    var dmData = File.ReadAllText("../Infrastructure/Data/SeedData/delivery.json");
    var deliveryMethods = JsonSerializer.Deserialize<List<DeliveryMethod>>(dmData);
    foreach (var item in deliveryMethods)
    {
        context.DeliveryMethods.Add(item);
    }
    await context.SaveChangesAsync();
}
}
```

#### 9.4. Creating the order migration

1.  In terminal from solution folder we add new migration:

```
dotnet ef migrations add OrderEntityAdded -p Infrastructure -s API -c StoreContext
```

#### 9.5. Creating an order service

1.  In `Core/Interfaces` create a new interface `IOrderService.cs`

```c#
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities.OrderAggregate;

namespace Core.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string buyerEmail, int deliveryMethod, string basketId, Address shippingAddress);
        Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail);
        Task<Order> GetOrderByIdAsync(int id, string buyerEmail);
        Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync();
    }
}
```

2.  In `Infrastructure/Services` create a new class `OrderService.cs`

```c#
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<DeliveryMethod> _dmRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IBasketRepository _basketRepo;

        public OrderService(IGenericRepository<Order> orderRepo, IGenericRepository<DeliveryMethod> dmRepo,
            IGenericRepository<Product> productRepo, IBasketRepository basketRepo)
        {
            _orderRepo = orderRepo;
            _dmRepo = dmRepo;
            _productRepo = productRepo;
            _basketRepo = basketRepo;
        }

        public async Task<Order> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, Address shippingAddress)
        {
            // get basket from the repo
            var basket = await _basketRepo.GetBasketAsync(basketId);
            // get items from the product repo
            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {
                var productItem = await _productRepo.GetByIdAsync(item.Id);
                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
                items.Add(orderItem);
            }
            // get delivery method from repo
            var deliveryMethod = await _dmRepo.GetByIdAsync(deliveryMethodId);
            // calc subtotal
            var subtotal = items.Sum(item => item.Price * item.Quantity);
            // create order
            var order = new Order(items, buyerEmail, shippingAddress, deliveryMethod, subtotal);
            // TODO: save to db

            // return order
            return order;
        }

        public Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
        {
            throw new System.NotImplementedException();
        }

        public Task<Order> GetOrderByIdAsync(int id, string buyerEmail)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
```

3.  In `ApplicationServicesExtensions.cs` add this as a service

```c#
services.AddScoped<IOrderService, OrderService>();
```

#### 9.5. Creating the order controller

1.  In `Api/Dtos` create a new class `OrderDto.cs`

```c#
namespace API.Dtos
{
    public class OrderDto
    {
        public string BasketId { get; set; }
        public int DeliveryMethodId { get; set; }
        public AddressDto ShipToAddress { get; set; }
    }
}
```

2.  Added new mapping profiles in `MappingProfiles.cs`

```c#
            CreateMap<AddressDto, Core.Entities.OrderAggregate.Address>();
```

3.  To extend claims method we create a new class `ClaimsPrincipleExtensions.cs` in `API/Extensions`

```c#
using System.Security.Claims;

namespace API.Extension
{
    public static class ClaimPrincipalExtensions
    {
        public static string RetrieveEmailFromPrincipal(this ClaimsPrincipal user)
        {
            return user?.FindFirstValue(ClaimTypes.Email);
        }
    }
}
```

4.  In `API/Controllers` create a new class `OrdersController.cs`

```c#
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Dtos;
using API.Errors;
using API.Extension;
using AutoMapper;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class OrdersController: BaseApiController
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public OrdersController(IOrderService orderService,IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(OrderDto orderDto)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var address = _mapper.Map<AddressDto, Address>(orderDto.ShipToAddress);
            var order = await _orderService.CreateOrderAsync(email, orderDto.DeliveryMethodId,orderDto.BasketId,address);
            if (order == null) return BadRequest(new ApiResponse(400,"Problem creating order"));
            return Ok(order);
        }
    }
}
```

#### 9.6. Implementing Unit Of Work Design Pattern

1.  In `Core/Interfaces` add new interface `IUnitOfWork.cs`

```c#
using System;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
        Task<int> Complete();
    }
}
```

2.  In `Infrastructure/Data` create a new class `UnitOfWork.cs`

```c#
using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Data
{
    public class UnitOfWork: IUnitOfWork
    {
        private readonly StoreContext _context;
        private Hashtable _repositories;

        public UnitOfWork(StoreContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
        {
            if (_repositories == null) _repositories = new Hashtable();

            var type = typeof(TEntity).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _context);
                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<TEntity>) _repositories[type];
        }

        public async Task<int> Complete()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
```

3.  In `ApplicationServiceExtensions.cs` add it as a service

```c#
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

#### 9.7. Updating the generic repository

1.  In `IGenericRepository` added following method declarations

```c#
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
```

2.  We implement following methods in `GenericRepository.cs`

```c#
public void Add(T entity)
{
    _context.Set<T>().Add(entity);
}

public void Update(T entity)
{
    _context.Set<T>().Attach(entity);
    _context.Entry(entity).State = EntityState.Modified;
}

public void Delete(T entity)
{
    _context.Set<T>().Remove(entity);
}
```

3.  We can now refactor `OrderServices.cs`

```c#
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IBasketRepository _basketRepo;
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IBasketRepository basketRepo, IUnitOfWork unitOfWork)
        {
            _basketRepo = basketRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<Order> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, Address shippingAddress)
        {
            // get basket from the repo
            var basket = await _basketRepo.GetBasketAsync(basketId);
            // get items from the product repo
            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {
                var productItem = await _unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
                items.Add(orderItem);
            }
            // get delivery method from repo
            var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(deliveryMethodId);
            // calc subtotal
            var subtotal = items.Sum(item => item.Price * item.Quantity);
            // create order
            var order = new Order(items, buyerEmail, shippingAddress, deliveryMethod, subtotal);
            _unitOfWork.Repository<Order>().Add(order);
            // save to db
            var result = await _unitOfWork.Complete();

            if (result <= 0) return null;

            // delete basket
            await _basketRepo.DeleteBasketAsync(basketId);
            // return order
            return order;
        }

        public Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
        {
            throw new System.NotImplementedException();
        }

        public Task<Order> GetOrderByIdAsync(int id, string buyerEmail)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
```

#### 9.8. Implementing the Order get methods

1.  Inside `Core/Specifications` create a new class `OrdersWithItemsAndOrderingSpecification.cs`

```c#
using Core.Entities.OrderAggregate;

namespace API.Specifications
{
    public class OrdersWithItemsAndOrderingSpecification : BaseSpecification<Order>
    {
        public OrdersWithItemsAndOrderingSpecification(string email) : base(o =>o.BuyerEmail == email)
        {
            AddInclude(o=>o.OrderItems);
            AddInclude(o=>o.DeliveryMethod);
            AddOrderByDescending(o=>o.OrderDate);
        }

        public OrdersWithItemsAndOrderingSpecification(int id, string email) :
            base(o=>o.Id == id && o.BuyerEmail ==email)
        {
            AddInclude(o=>o.OrderItems);
            AddInclude(o=>o.DeliveryMethod);
        }

    }
}
```

2.  We can now implement the missing members in `OrderService.cs`

```c#
public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
{
    var spec = new OrdersWithItemsAndOrderingSpecification(buyerEmail);
    return await _unitOfWork.Repository<Order>().ListAsync(spec);
}

public async Task<Order> GetOrderByIdAsync(int id, string buyerEmail)
{
    var spec = new OrdersWithItemsAndOrderingSpecification(id, buyerEmail);
    return await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
}

public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
{
    return await _unitOfWork.Repository<DeliveryMethod>().ListAllAsync();
}
```

#### 9.9. Implementing order controller get methods

1.  In `OrderController.cs` we add following methods:

```c#
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Order>>> GetOrdersForUser()
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var orders = await _orderService.GetOrdersForUserAsync(email);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrderByIdForUser(int id)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var order = await _orderService.GetOrderByIdAsync(id, email);
            if (order == null) return NotFound(new ApiResponse(404));
            return order;
        }

        [HttpGet("deliveryMethods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethods()
        {
            return Ok(await _orderService.GetDeliveryMethodsAsync());
        }
```

2.  If we test `GetOrders()` method with postman and send request to api/orders endpoint we get an error:

```json
{
	"details": "   at Microsoft.EntityFrameworkCore.Sqlite.Query.Internal.SqliteQueryableMethodTranslatingExpressionVisitor.TranslateOrderBy(ShapedQueryExpression source, LambdaExpression keySelector, Boolean ascending)\r\n   at Microsoft.EntityFrameworkCore.Query.QueryableMethodTranslatingExpressionVisitor.VisitMethodCall(MethodCallExpression methodCallExpression)\r\n   at System.Linq.Expressions.MethodCallExpression.Accept(ExpressionVisitor visitor)\r\n   at System.Linq.Expressions.ExpressionVisitor.Visit(Expression node)\r\n   at Microsoft.EntityFrameworkCore.Query.QueryableMethodTranslatingExpressionVisitor.VisitMethodCall(MethodCallExpression methodCallExpression)\r\n   at System.Linq.Expressions.MethodCallExpression.Accept(ExpressionVisitor visitor)\r\n   at System.Linq.Expressions.ExpressionVisitor.Visit(Expression node)\r\n   at Microsoft.EntityFrameworkCore.Query.QueryableMethodTranslatingExpressionVisitor.VisitMethodCall(MethodCallExpression methodCallExpression)\r\n   at System.Linq.Expressions.MethodCallExpression.Accept(ExpressionVisitor visitor)\r\n   at System.Linq.Expressions.ExpressionVisitor.Visit(Expression node)\r\n   at Microsoft.EntityFrameworkCore.Query.QueryCompilationContext.CreateQueryExecutor[TResult](Expression query)\r\n   at Microsoft.EntityFrameworkCore.Storage.Database.CompileQuery[TResult](Expression query, Boolean async)\r\n   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.CompileQueryCore[TResult](IDatabase database, Expression query, IModel model, Boolean async)\r\n   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.<>c__DisplayClass12_0`1.<ExecuteAsync>b__0()\r\n   at Microsoft.EntityFrameworkCore.Query.Internal.CompiledQueryCache.GetOrAddQuery[TResult](Object cacheKey, Func`1 compiler)\r\n   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.ExecuteAsync[TResult](Expression query, CancellationToken cancellationToken)\r\n   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryProvider.ExecuteAsync[TResult](Expression expression, CancellationToken cancellationToken)\r\n   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryable`1.GetAsyncEnumerator(CancellationToken cancellationToken)\r\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IncludableQueryable`2.GetAsyncEnumerator(CancellationToken cancellationToken)\r\n   at System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable`1.GetAsyncEnumerator()\r\n   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)\r\n   at Infrastructure.Data.GenericRepository`1.ListAsync(ISpecification`1 spec) in C:\\Users\\korisnik\\Desktop\\dev\\web\\ecommerce-shop\\Infrastructure\\Data\\GenericRepository.cs:line 37\r\n   at Infrastructure.Services.OrderService.GetOrdersForUserAsync(String buyerEmail) in C:\\Users\\korisnik\\Desktop\\dev\\web\\ecommerce-shop\\Infrastructure\\Services\\OrderService.cs:line 56\r\n   at API.Controllers.OrdersController.GetOrdersForUser() in C:\\Users\\korisnik\\Desktop\\dev\\web\\ecommerce-shop\\API\\Controllers\\OrdersController.cs:line 41\r\n   at lambda_method33(Closure , Object )\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.AwaitableObjectResultExecutor.Execute(IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeActionMethodAsync>g__Awaited|12_0(ControllerActionInvoker invoker, ValueTask`1 actionResultValueTask)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeNextActionFilterAsync>g__Awaited|10_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeInnerFilterAsync()\r\n--- End of stack trace from previous location ---\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeFilterPipelineAsync>g__Awaited|19_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\r\n   at Microsoft.AspNetCore.Routing.EndpointMiddleware.<Invoke>g__AwaitRequestTask|6_0(Endpoint endpoint, Task requestTask, ILogger logger)\r\n   at Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIMiddleware.Invoke(HttpContext httpContext)\r\n   at Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware.Invoke(HttpContext httpContext, ISwaggerProvider swaggerProvider)\r\n   at Microsoft.AspNetCore.Authorization.Policy.AuthorizationMiddlewareResultHandler.HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)\r\n   at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)\r\n   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)\r\n   at Microsoft.AspNetCore.Diagnostics.StatusCodePagesMiddleware.Invoke(HttpContext context)\r\n   at API.Middleware.ExceptionMiddleware.InvokeAsync(HttpContext context) in C:\\Users\\korisnik\\Desktop\\dev\\web\\ecommerce-shop\\API\\Middleware\\ExceptionMiddleware.cs:line 29",
	"statusCode": 500,
	"message": "SQLite cannot order by expressions of type 'DateTimeOffset'. Convert the values to a supported type or use LINQ to Objects to order the results."
}
```

3.  To fix this we go to `OnModelCreating` method in `StoreContext.cs`

```c#
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    // check if API is using Sqlite database
    if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            //get all decimal values from database
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(decimal));

            foreach (var property in properties)
            {
                // dealing with decimal property
                modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();
            }
            // fix sql issue
            var dateTimeProperties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTimeOffset));

            foreach (var property in dateTimeProperties)
            {
                modelBuilder.Entity(entityType.Name).Property(property.Name)
                    .HasConversion(new DateTimeOffsetToBinaryConverter());
            }
        }
    }
```

#### 9.10. Shaping the order data

1.  Inside `API/Dtos` create a new class `OrderToReturnDto.cs`

```c#
using System;
using System.Collections.Generic;
using Core.Entities.OrderAggregate;

namespace API.Dtos
{
    public class OrderToReturnDto
    {
        public int Id { get; set; }
        public string BuyerEmail { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.Now;
        public Address ShipToAddress { get; set; }
        public string DeliveryMethod { get; set; }
        public decimal ShippingPrice { get; set; }
        public IReadOnlyList<OrderItem> OrderItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
    }
}
```

2.  In the same directory we create another new dto class `OrderItemDto`

```c#
namespace API.Dtos
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string PictureUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
```

3.  Now in `MappingProfiles.cs` we add following profiles:

```c#
CreateMap<Order, OrderToReturnDto>();
CreateMap<OrderItem, OrderItemDto>();
```

4.  Now in `OrderController.cs` we refactor returning objects

#### 9.11. Automapper config for orders we update our mappers:

1.  Go to `MappingProfiles.cs`

```c#
CreateMap<Order, OrderToReturnDto>()
                .ForMember(d => d.DeliveryMethod, o => o.MapFrom(s => s.DeliveryMethod.ShortName))
                .ForMember(d => d.ShippingPrice, o => o.MapFrom(s => s.DeliveryMethod.Price));
CreateMap<OrderItem, OrderItemDto>()
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ItemOrdered.ProductItemId))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.ItemOrdered.ProductName))
                .ForMember(d => d.PictureUrl, o => o.MapFrom(s => s.ItemOrdered.PictureUrl))
```

#### To include full URL of pictureUrl property we go to to `ProductUrlResolver.cs`

1.  Create a new class `OrderItemUrlResolver.cs` in `API/Helpers`

```c#
using API.Dtos;
using AutoMapper;
using AutoMapper.Configuration;
using Core.Entities.OrderAggregate;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace API.Helpers
{
    public class OrderItemUrlResolver : IValueResolver<OrderItem,OrderItemDto,string>
    {
        private readonly IConfiguration _config;

        public OrderItemUrlResolver(IConfiguration config)
        {
            _config = config;
        }

        public string Resolve(OrderItem source, OrderItemDto destination, string destMember, ResolutionContext context)
        {
            if (!string.IsNullOrEmpty(source.ItemOrdered.PictureUrl))
            {
                return _config["ApiUrl"] + source.ItemOrdered.PictureUrl;
            }

            return null;
        }
    }
}
```

2.  Now we need to include this in our mapping profiles

```c#
CreateMap<OrderItem, OrderItemDto>()
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ItemOrdered.ProductItemId))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.ItemOrdered.ProductName))
                .ForMember(d => d.PictureUrl, o => o.MapFrom(s => s.ItemOrdered.PictureUrl))
                .ForMember(d => d.PictureUrl, o => o.MapFrom<OrderItemUrlResolver>());
```
