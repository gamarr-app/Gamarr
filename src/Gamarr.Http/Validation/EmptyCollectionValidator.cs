using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;

namespace Gamarr.Http.Validation
{
    public class EmptyCollectionValidator<T, TElement> : PropertyValidator<T, IEnumerable<TElement>>
    {
        public override string Name => "EmptyCollectionValidator";

        public override bool IsValid(ValidationContext<T> context, IEnumerable<TElement> value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Empty();
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Collection Must Be Empty";
    }
}
