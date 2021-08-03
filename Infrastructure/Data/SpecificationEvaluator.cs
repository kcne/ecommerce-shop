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
            //add Criteria to query expression
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria); // ex. p=>p.ProductTypeId==id
            }
            //add OrderBy to query expression
            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy); 
            }
            //add OrderByDesc to query expression
            if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending); 
            }
            //add paging
            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip).Take(spec.Take);
            }
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
            return query;
        }
    }
}