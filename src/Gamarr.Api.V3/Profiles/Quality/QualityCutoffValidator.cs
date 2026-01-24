using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Gamarr.Api.V3.Profiles.Quality
{
    public static class QualityCutoffValidator
    {
        public static IRuleBuilderOptions<T, int> ValidCutoff<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new ValidCutoffValidator<T>());
        }
    }

    public class ValidCutoffValidator<T> : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Cutoff must be an allowed quality or group";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var cutoff = (int)context.PropertyValue;
            dynamic instance = context.ParentContext.InstanceToValidate;
            var items = instance.Items as IList<QualityProfileQualityItemResource>;

            if (items == null || !items.Any())
            {
                return false;
            }

            var cutoffItem = items.SingleOrDefault(i => (i.Quality == null && i.Id == cutoff) || i.Quality?.Id == cutoff);

            if (cutoffItem == null)
            {
                context.MessageFormatter.AppendArgument("ValidationMessage",
                    $"Cutoff ID '{cutoff}' does not match any quality or group in the profile");
                return false;
            }

            if (!cutoffItem.Allowed)
            {
                context.MessageFormatter.AppendArgument("ValidationMessage",
                    $"Cutoff quality/group '{cutoffItem.Name ?? cutoffItem.Quality?.Name ?? cutoff.ToString()}' must be allowed");
                return false;
            }

            return true;
        }
    }
}
