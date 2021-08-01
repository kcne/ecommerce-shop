# Development Procces Tracker:
#### Online store web application as a part of a web development course. 
To track progress I embedded all of the development process(code edits, file creation, design pattern implementation etc.) in this file  so in the end I will have whole development procces documented.
### Stack: .NET 5.0, Angular 11 and SQLite.
### Enviroment: Windows 10
### Date Created: 30.7.2021.
### Last Edit: 31.7.2021
### Contributors: Emin Kocan
# API:
## 1. API Basics:
### 1.1. Made a Skeleton API
##Created project and structured it so it is split in  3 parts:
 - API: Main project in which we store controllers, `Program.cs` , `Startup.cs` & configuration files.
 - Core: A place where we will store our `Entities` & `Interfaces`.
 - Infrastructure: This is where all data related parts of application like Entity Framework configuration files, migrations, .json data files, and other classes like Repository & Data Context classes will be stored.
## 2. API Architecture:
### 2.1. Repository Pattern: Added Repository(Data/ProductRepository.cs) and Interface(Data/IProductRepository.cs) classes
### 2.2. Added Repository Methods:
#### 2.2.1. Added files: IProductRepository.cs, ProductRepostiory.cs
#### 2.2.2. Implemented Methods:
```c#
Task<Product> GetProductByIdAsync(int id);
Task<IReadOnlyList<Product>> GetProductsAsync();
```
#### 2.2.3. Added Repository as a service in Startup.cs:
```c#
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
#### 2.2.4. Implemented constructor to ProductRepository to take readonly StoreContext _context variable as parameter
```c#
 private readonly StoreContext _context;

        public ProductRepository(StoreContext context)
        {
            _context = context;
        }
 ```
#### 2.2.5. Implemented ProductRepository methods:
```c#
public async Task<Product> GetProductByIdAsync(int id)
        {
        //Commented out code added later due to later documentation
            return await _context.Products
                //.Include(p => p.ProductType)
                //.Include(p => p.ProductBrand)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IReadOnlyList<Product>> GetProductsAsync()
        {
            return await _context.Products
                //.Include(p=>p.ProductType)
                //.Include(p=>p.ProductBrand)
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
### 2.4. Create a new migration for entities
```c#
//Dropping the old database: 
dotnet ef migrations remove -p Infrastructure -s API
//Removing the old migrations:
dotnet ef migrations remove -p Infrastructure -s API
//Creating new initial migration:
dotnet ef migrations add InitialCreate -p Infrastructure -s API -o Data/Migrations
```
### 2.5. Configuration of the migrations
#### 2.5.1. Created New Directory "Config" and added Product Configuration Class(ProductConfiguration.cs)
#### 2.5.2. Derrived ProductConfiguration class from Entity Type Configuration interface: 
```c#
//Data/Config/ProductConfiguration.cs
public class ProductRepository:IProductRepository{}
```
#### 2.5.3. Implemented Configuration Method:
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
#### 2.7.1. Created new directory `Data/SeedData` with and added files ``brands.json`` `types.json` & `products.json`
#### 2.7.2. Added New Class `Data/StoreContextSeed.cs`
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
#### 2.7.3. Updated Program.cs
```c#
 try
{
var context = services.GetRequiredService<StoreContext>();
//Next two lines
await context.Database.MigrateAsync();
await StoreContextSeed.SeedAsync(context,loggerFactory);
}
```
### 2.8. Added code to get product brands and types along with Product object
#### 2.8.1. Updated `IProductRepository.cs`
```c#
//..Core/Interfaces/IProductRepository.cs
//Added following methods:
Task<IReadOnlyList<ProductBrand>> GetProductsBrandsAsync();
Task<IReadOnlyList<ProductType>> GetProductsTypesAsync();
```
#### 2.8.2.Implemented methods in `ProductRepository.cs`
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
#### 2.8.3. Updated `ProductController.cs`
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