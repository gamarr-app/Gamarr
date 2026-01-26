using FluentValidation;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Validation
{
    public static class UrlValidation
    {
        public static IRuleBuilderOptions<T, string> IsValidUrl<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(value => value != null && value.IsValidUrl())
                              .WithMessage("Invalid Url: '{PropertyValue}'");
        }
    }
}
