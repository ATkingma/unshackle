using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Shop;

// Kavita's ProjectToExtensions.ProjectToWithProgress: the caller picks TDestination, so no single map is known.
public static class ProjectToExtensions
{
    public static IQueryable<TDestination> ProjectToWithProgress<TSource, TDestination>(this IQueryable<TSource> queryable, IMapper mapper, int userId) =>
        queryable.ProjectTo<TDestination>(mapper.ConfigurationProvider, new { userId });
}
