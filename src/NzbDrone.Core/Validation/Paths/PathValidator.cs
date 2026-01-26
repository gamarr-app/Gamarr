using FluentValidation;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Validation.Paths
{
    public static class PathValidation
    {
        public static IRuleBuilderOptions<T, string> IsValidPath<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Must(value => value != null && value.IsPathValid(PathValidationType.CurrentOs))
                              .WithMessage("Invalid Path: '{PropertyValue}'");
        }
    }
}
