using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Shop;

// The paging helper of cats (QueryableExtensions.ProjectToPaginatedDataAsync): the caller picks TResult.
public static class QueryableExtensions
{
    public static List<TResult> ProjectToPage<T, TResult>(this IQueryable<T> query, IConfigurationProvider configuration, int pageNumber, int pageSize) =>
        query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<TResult>(configuration)
            .ToList();
}
