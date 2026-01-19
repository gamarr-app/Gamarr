namespace NzbDrone.Core.MediaFiles.GameImport;

public enum ImportRejectionReason
{
    Unknown,
    FileLocked,
    UnknownGame,
    DangerousFile,
    ExecutableFile,
    ArchiveFile,
    GameFolder,
    InvalidFilePath,
    UnsupportedExtension,
    InvalidGame,
    UnableToParse,
    Error,
    DecisionError,
    GameAlreadyImported,
    MinimumFreeSpace,
    NoAudio,
    GameNotFoundInRelease,
    Sample,
    SampleIndeterminate,
    Unpacking,
    MultiPartGame,
    NotQualityUpgrade,
    NotRevisionUpgrade,
    NotCustomFormatUpgrade
}
