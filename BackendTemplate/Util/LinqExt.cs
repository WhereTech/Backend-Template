using System.Linq.Expressions;

namespace BackendTemplate.Util;

public static class LinqExt
{
    public static IQueryable<TSource> WhereIF<TSource>(
        this IQueryable<TSource> source,
        bool condition,
        Expression<Func<TSource, bool>> predicate) {
        return condition ? source.Where(predicate) : source;
    }
}