namespace NzbDrone.Core.MediaFiles.Archives
{
    /// <summary>
    /// The result of a cheap header-only peek into an archive-wrapped release:
    /// the archive on disk plus the single game file hiding inside it.
    /// Transient — it lives on <see cref="Parser.Model.LocalGame"/> for the
    /// duration of one import and is never persisted.
    /// </summary>
    public class GameArchiveInspection
    {
        /// <summary>
        /// The archive on disk. For a folder-shaped release this is the archive
        /// found inside the release folder, not the folder itself.
        /// </summary>
        public string ArchivePath { get; set; }

        /// <summary>
        /// The entry key of the game file inside the archive, e.g.
        /// "Kirby's Return to Dream Land Deluxe [0100...][v0].xci".
        /// </summary>
        public string EntryName { get; set; }

        /// <summary>
        /// The <b>uncompressed</b> size of <see cref="EntryName"/>. This is what
        /// the import pipeline must budget disk space and compare quality on —
        /// the compressed archive size is meaningless once extracted.
        /// </summary>
        public long EntrySize { get; set; }

        public override string ToString()
        {
            return $"{ArchivePath}!{EntryName}";
        }
    }
}
