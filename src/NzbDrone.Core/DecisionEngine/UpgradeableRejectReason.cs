namespace NzbDrone.Core.DecisionEngine
{
    public enum UpgradeableRejectReason
    {
        None,
        BetterQuality,
        BetterRevision,
        BetterVersion,
        QualityCutoff,
        CustomFormatScore,
        CustomFormatCutoff,
        MinCustomFormatScore,
        UpgradesNotAllowed
    }
}
