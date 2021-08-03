using Core.Entities;

namespace API.Specifications
{
    public class ProductsWithTypesAndBrandsSpecification : BaseSpecification<Product>
    {
        public ProductsWithTypesAndBrandsSpecification(ProductSpecParams productParams)
            : base(x =>
                        (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search))&& 
                        (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
                        (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId))
        {
        AddInclude(x => x.ProductType);
            AddInclude(x => x.ProductBrand);
            AddOrderBy(x => x.Name);
            ApplyPaging(productParams.PageSize*(productParams.PageIndex-1),productParams.PageSize);
            if (!string.IsNullOrEmpty(productParams.Sort))
                switch (productParams.Sort)
                {
                    // by ascending price
                    case "priceAsc":
                        AddOrderBy(p => p.Price);
                        break;
                    // by descending price
                    case "priceDesc":
                        AddOrderByDescending(p => p.Price);
                        break;
                    default:
                        //by name
                        AddOrderBy(n => n.Name);
                        break;
                }
        }

        public ProductsWithTypesAndBrandsSpecification(int id) : base(x => x.Id == id)
        {
            AddInclude(x => x.ProductType);
            AddInclude(x => x.ProductBrand);
        }
    }
}