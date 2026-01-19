using System.Text.Json.Serialization;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Profiles.Languages
{
    public class LanguageResource : RestResource
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public new int Id { get; set; }
        public string Name { get; set; }
        public string NameLower => Name.ToLowerInvariant();
    }
}
