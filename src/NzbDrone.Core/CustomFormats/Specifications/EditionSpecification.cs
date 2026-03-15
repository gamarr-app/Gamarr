namespace NzbDrone.Core.CustomFormats
{
    public class EditionSpecification : RegexSpecificationBase
    {
        public override int Order => 2;
        public override string ImplementationName => "Edition";
        public override string InfoLink => "https://github.com/gamarr-app/Gamarr/wiki/settings#custom-formats-2";

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            return MatchString(input.GameInfo.Edition);
        }
    }
}
