using System;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using NzbDrone.Common.Reflection;

namespace NzbDrone.Core.Datastore
{
    public static class MappingExtensions
    {
        public static PropertyInfo GetMemberName<T, TChild>(this Expression<Func<T, TChild>> member)
        {
            var body = member.Body;

            // Value-typed properties arrive wrapped in a conversion node. The old code
            // assumed anything that wasn't a MemberExpression was a UnaryExpression over
            // one, and dereferenced the result of two unchecked "as" casts -- the same
            // unguarded-cast pattern that made the where-clause builder throw a
            // NullReferenceException on a filtered history/blocklist query.
            while (body is UnaryExpression unary)
            {
                body = unary.Operand;
            }

            if (body is not MemberExpression memberExpression)
            {
                throw new ArgumentException($"Expression '{member}' does not refer to a property.", nameof(member));
            }

            if (memberExpression.Member is not PropertyInfo propertyInfo)
            {
                throw new ArgumentException($"Expression '{member}' refers to a field, not a property.", nameof(member));
            }

            return propertyInfo;
        }

        public static bool IsMappableProperty(this MemberInfo memberInfo)
        {
            var propertyInfo = memberInfo as PropertyInfo;

            if (propertyInfo == null)
            {
                return false;
            }

            if (!propertyInfo.IsReadable() || !propertyInfo.IsWritable())
            {
                return false;
            }

            // LookupDbType is obsolete ("for internal use only") but no public replacement exists in Dapper 2.x
#pragma warning disable CS0618
            SqlMapper.LookupDbType(propertyInfo.PropertyType, "", false, out var handler);
#pragma warning restore CS0618
            if (propertyInfo.PropertyType.IsSimpleType() || handler != null)
            {
                return true;
            }

            return false;
        }
    }
}
