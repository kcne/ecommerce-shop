## Project Development Procces Tracker:
#### Project: Online store web application as a part of a web development course. 
Goal: To track progress I embedded all of the development process(code edits, file creation, design pattern implementation etc.) in this file  so in the end I will have whole development procces documented.
### Stack: .NET 5.0, Angular 11 and SQLite.
### Enviroment: Windows 10
### Date Created: 30.7.2021.
### Last Edit: 11.8.2021 22:35
### Contributors: Emin Kočan
# API:
## 1. API Basics:
### 1.1. Made a Skeleton API
### Created project and structured it so it is split in  3 parts:
 - API: Main project in which we store controllers, `Program.cs` , `Startup.cs` & configuration files.
 - Core: A place where we will store our `Entities` & `Interfaces`.
 - Infrastructure: This is where all data related parts of application like Entity Framework configuration files, migrations, .json data files, and other classes like Repository & Data Context classes will be stored.
## 2. API Architecture:
### 2.1. Repository Pattern:
### 2.2. Added Repository Methods:
 1. Added classes: `IProductRepository.cs` in `Interfaces`, `ProductRepository.cs` in `Data`
 2. Declared following methods in `IProductRepository.cs`:
```c#
//..Interfaces/IProductRepository.cs
Task<Product> GetProductByIdAsync(int id);
Task<IReadOnlyList<Product>> GetProductsAsync();
```
 3. Added ProductRepository as a service in `Startup.cs`:
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
 4. Create a constructor `ProductRepository(Storecontext context)` to take readonly StoreContext variable on initial creation:
```c#
 private readonly StoreContext _context;

        public ProductRepository(StoreContext context)
        {
            _context = context;
        }
 ```
 5. Implemented following ProductRepository methods:
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
 1. Created new directory `Config` and added `ProductConfiguration.cs` class
 2. Derived `ProductConfiguration` class from `IEntityTypeConfiguration<>` interface 
```c#
//Data/Config/ProductConfiguration.cs
public class ProductConfiguration:IEntityTypeConfiguration<Product>
```
 3. Implemented `Configure` method:
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
#### 1. Created new directory `Data/SeedData` with and added files ``brands.json`` `types.json` & `products.json`
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
 1. Create new `Specifications` directory in `Core` project
 2. Create new `ISpecification.cs` interface in `Specifications` dir
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
 3. Create a new class `BaseSpecification.cs` in `Specifications`
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
 5. Added following methods in `IGenericRepository.cs`
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
 1. Generate an empty constructor in `BaseSpecification.cs`
 2. Create `ProductsWithTypesAndBrandsSpecification.cs` in `Specifications`
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
 1. Overload a constructor in `BaseSpecification.cs` to take `criteria` as a paremeter:
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
 1. Create dir `Dtos` in `API`
 2. Create a class `ProductToReturnDto.cs` in `Dtos`
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
 1. Install `AutoMapper.Extensions.Microsoft.DependencyInjection` NuGet package.
 2. Create a Helpers folder in `API` directory.
 3. In `Helpers` folder create `MappingProfiles.cs` file
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
 4. Add AutoMapper as a service in `Startup.cs`
```c#
services.AddAutoMapper(typeof(MappingProfiles));
```
 5. Add `IMapper` to `ProductsController` constructor:
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
 6. Refactor `GetProduct(int id)` & `GetProducts()` methods in Products Controller
```c#
public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id)
{
    var spec = new ProductsWithTypesAndBrandsSpecification(id);
    var product = await _productsRepo.GetEntityWithSpec(spec);
    return _mapper.Map<Product, ProductToReturnDto>(product);
}
```
 7. Result after calling the method from the url `https://localhost:5001/api/products/2`:
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
 1. Add ```"ApiUrl": "https://localhost:5001/"```  in `appsettings.Development.json`
 2. Create `ProductUrlResolver.cs` class in `Helpers`
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
 1. Create new folder `wwwroot` in `API` directory
 2. Copy `images` folder to `wwwroot` directory
 3. In `Startup.cs` class inside `Configure(IApplicationBuilder app, IWebHostEnviroment env)` method add following line:
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
 1. Create `BaseApiController.cs` class in `Controllers`
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
 2. Create new `BuggyController.cs` controller in `Controllers`
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
 1. Inside `API` create folder `Errors`
 2. In `Errors` folder create a new class `ApiResponse`
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
 4. In `Controllers` create new `ErrorController.cs` class
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
 5. Added following line in `Configure()` method inside`Startup.cs` class
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
 6. Now if we hit endpoint that doesn't exist from the browser eg. https://localhost:5001/api/endpointthatdoesnotexist we get a response:
```json
{
    "statusCode": 404,
    "message": "Resource not found"
}
```
### 4.2. Creating Exception handler middleware
 1. In `Errors` folder create a new class `ApiException`
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
 2. Inside `API` folder create new folder `Middleware`
 3. Inside `Middleware` create `ExceptionMiddleware.cs` class
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
 3. Add `ExceptionMiddleware` in `Startup` config:

 We replace:
```c#
if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
```
with:
```c#
app.UseMiddleware<ExceptionMiddleware>();
```
 4. Postman response with the https://localhost:5001/api/buggy/servererror url:
```json
{
  "Details": "   at API.Controllers.BuggyController.GetServerError() in C:\\Users\\korisnik\\Desktop\\dev\\ecommerce-shop\\API\\Controllers\\BuggyController.cs:line 31\r\n   at lambda_method12(Closure , Object , Object[] )\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.SyncActionResultExecutor.Execute(IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeActionMethodAsync()\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeNextActionFilterAsync()\r\n--- End of stack trace from previous location ---\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeInnerFilterAsync()\r\n--- End of stack trace from previous location ---\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeFilterPipelineAsync>g__Awaited|19_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)\r\n   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)\r\n   at Microsoft.AspNetCore.Routing.EndpointMiddleware.<Invoke>g__AwaitRequestTask|6_0(Endpoint endpoint, Task requestTask, ILogger logger)\r\n   at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)\r\n   at Microsoft.AspNetCore.Diagnostics.StatusCodePagesMiddleware.Invoke(HttpContext context)\r\n   at API.Middleware.ExceptionMiddleware.InvokeAsync(HttpContext context) in C:\\Users\\korisnik\\Desktop\\dev\\ecommerce-shop\\API\\Middleware\\ExceptionMiddleware.cs:line 29",
  "StatusCode": 500,
  "Message": "Object reference not set to an instance of an object."
}
```
### 4.3. Improving validation error responses
 1. Inside `Errors` create a new class `ApiValidationErrorResponse.cs`
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
 2. In `Startup.cs` at the end of `public void ConfigureServices(IServiceCollection services)` add following lines:
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
 1. Install required packages if not installed already. See `API.sln` for more info.
 2. Add following lines in `Configure` and `ConfigureServices` methods in `Startup.cs` <br><br>

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
 3. Added following line in `ErrorController.cs`
```c#
    [Route("errors/{code}")]
 //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
    [ApiExplorerSettings(IgnoreApi=true)]
//-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
    public class ErrorController : BaseApiController{...}
```
 4. Added following line in `GetProduct(int id)` method inside `ProductsController` so it returns an error page if we enter non-existing id of the product:
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
 6. Now if we send a request to our localhost https://localhost:5001/api/products?sort=priceAsc we get an exception:
```json
{
    "details": "...",
    "statusCode": 500,
    "message": "SQLite cannot order by expressions of type 'decimal'. Convert the values to a supported type or use LINQ to Objects to order the results."
}
```
#### 5.2. Solving the decimal problem with SQlite
 1. Refactor `OnModelCreating(ModelBuilder modelBuilder)` method inside `StoreContext.cs` class
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
 1. Modified `GetProducts()` method so it now takes two more optional arguments `typeId` & `brandId`
```c#
public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetProducts(
            string sort,int? brandId,int? typeId)
```
2. Modified  public `ProductsWithTypesAndBrandsSpecification.cs` class constructor
```c#
 public ProductsWithTypesAndBrandsSpecification(string sort,int? brandId,int? typeId) 
  //added following:
            : base(x=>(!brandId.HasValue || x.ProductBrandId == brandId) &&
                   (!typeId.HasValue || x.ProductTypeId==typeId))
```
#### 5.4. Adding Pagination
 1. Added properties in `ISpecification.cs` interface
```c#
// We need following properties for our pagination mechanism
// Take number of products
int Take { get; }
// Skip number of products
int Skip { get; }
// Pagination on/off
bool IsPagingEnabled { get; }
```
 2. Implemented new properties and added `ApplyPaging(int,int)` method
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
 3. Configured `GetQuery()` method in `SpecificEvaluator` class
```c#
 // added following lines
 // add paging
if (spec.IsPagingEnabled)
{
    query = query.Skip(spec.Skip).Take(spec.Take);
}
```
 4. To clean up `GetProducts(string,int,int)` method and add constraints to page mechanism properties,<br>new class `ProductSpecParams.cs`  in `..\Core\Specifications\` is created:
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
 5. Now we can call `ProductSpecParams` object instead of arguments
```c#
public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetProducts([FromQuery]ProductSpecParams productParams)
        {
            var spec = new ProductsWithTypesAndBrandsSpecification(productParams);
            ...
         }
```
 6. Refactor `ProductWithTypesAndBrandSpecification.cs`(fix errors)
 7. Added paging to `ProductsWithTypesAndBrandsSpecification` constructor
```c#
AddInclude(x => x.ProductType);
AddInclude(x => x.ProductBrand);
AddOrderBy(x => x.Name);
// Added paging
ApplyPaging(productParams.PageSize*(productParams.PageIndex-1),productParams.PageSize);
```
 8. Inside `Helpers` folder in `API` create a class `Pagination.cs`
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
 9. In IGenericRepository added following method:
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
 1. Added following _search field in `ProductSpecParams.cs`
```c#
private string _search;

        public string Search
        {
            get=>_search;
            set=>_search=value.ToLower();
        }
```
 2. Refactored the `ProductWithTypesAndBrandsConstructor`
```c#
public ProductsWithTypesAndBrandsSpecification(ProductSpecParams productParams)
            : base(x =>
                        (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search))&& 
                        (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
                        (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId))
```
 3. And also `ProductWithFiltersForCountSpecification`
```c#
        public ProductWithFiltersForCountSpecification(ProductSpecParams productParams) 
            :base(x =>
        (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search))&& 
        (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
        (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId)) 
```
### 5.6. Adding CORS Support to the API
 1. In `ConfigureServices()` method inside `Startup.cs` add a new service
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
||||||| ea6ef4d
=======
### 4.4. Cleaning up `Startup.cs` file
 1. Create new directory `Extensions` in `API`
 2. Add new class `ApplicationServicesExtensions` in `Extensions` directory
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
 3. Inside `Extensions` create new class `SwaggerServicesExtensions`
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
 4. In `Startup.cs` delete lines copied to files above and use created method instead so now we cleaner `Startup.cs` file:
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

