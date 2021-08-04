using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;

namespace API.Specifications
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>> Criteria { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        Expression<Func<T,object>> OrderBy { get; }
        Expression<Func<T,object>> OrderByDescending { get; }
        // We need following properties for our pagination mechanism
        // Take number of products
        int Take { get; }
        // Skip number of products
        int Skip { get; }
        // Pagination on/off
        bool IsPagingEnabled { get; }
        
    }
}