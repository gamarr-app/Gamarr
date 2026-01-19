using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.Simkl
{
    public class SimklGameIdsResource
    {
        public int Simkl { get; set; }
        public string Imdb { get; set; }
        public string Igdb { get; set; }
    }

    public class SimklGamePropsResource
    {
        public string Title { get; set; }
        public int? Year { get; set; }
        public SimklGameIdsResource Ids { get; set; }
    }

    public class SimklGameResource
    {
        public SimklGamePropsResource Game { get; set; }
    }

    public class SimklResponse
    {
        public List<SimklGameResource> Games { get; set; }
    }

    public class RefreshRequestResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class UserSettingsResponse
    {
        public SimklUserResource User { get; set; }
        public SimklUserAccountResource Account { get; set; }
    }

    public class SimklUserResource
    {
        public string Name { get; set; }
    }

    public class SimklUserAccountResource
    {
        public int Id { get; set; }
    }

    public class SimklSyncActivityResource
    {
        public SimklGamesSyncActivityResource Games { get; set; }
    }

    public class SimklGamesSyncActivityResource
    {
        public DateTime All { get; set; }
    }
}
