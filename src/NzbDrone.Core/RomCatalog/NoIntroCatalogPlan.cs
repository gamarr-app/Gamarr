using System.Collections.Generic;

namespace NzbDrone.Core.RomCatalog
{
    public class NoIntroCatalogPlan
    {
        public List<NoIntroCatalogGamePlan> Games { get; set; } = new List<NoIntroCatalogGamePlan>();
        public List<NoIntroCatalogStandalonePlan> StandaloneGames { get; set; } = new List<NoIntroCatalogStandalonePlan>();
    }

    public class NoIntroCatalogGamePlan
    {
        public string SystemKey { get; set; }
        public string GameTitle { get; set; }
        public List<NoIntroCatalogComponentSlot> RegionLanguageComponents { get; set; } = new List<NoIntroCatalogComponentSlot>();
        public List<NoIntroCatalogComponentSlot> DownloadPlayComponents { get; set; } = new List<NoIntroCatalogComponentSlot>();
    }

    public class NoIntroCatalogStandalonePlan
    {
        public string Title { get; set; }
        public NoIntroRomComponentType ComponentType { get; set; }
    }

    public class NoIntroCatalogComponentSlot
    {
        public string SlotLabel { get; set; }
        public string CanonicalName { get; set; }
        public NoIntroRomComponentType ComponentType { get; set; }
    }
}
