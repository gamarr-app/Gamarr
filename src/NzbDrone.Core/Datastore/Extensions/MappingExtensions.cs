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
            if (!(member.Body is MemberExpression memberExpression))
            {
                memberExpression = (member.Body as UnaryExpression).Operand as MemberExpression;
            }

            return (PropertyInfo)memberExpression.Member;
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
