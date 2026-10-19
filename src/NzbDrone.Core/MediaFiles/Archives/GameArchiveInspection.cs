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
        /// <b>Null</b> in <see cref="ExtractWholeArchive"/> mode — there is no
        /// single entry then, the whole tree is the game.
        /// </summary>
        public string EntryName { get; set; }

        /// <summary>
        /// The <b>uncompressed</b> size of <see cref="EntryName"/>, or in
        /// <see cref="ExtractWholeArchive"/> mode the sum of every entry's
        /// uncompressed size. This is what the import pipeline must budget disk
        /// space and compare quality on — the compressed archive size is
        /// meaningless once extracted.
        /// </summary>
        public long EntrySize { get; set; }

        /// <summary>
        /// Whole-archive mode: the archive holds no single extractable ROM entry
        /// but the game imports at folder granularity anyway (PC / unknown
        /// platform), so every entry is extracted into the game folder and the
        /// folder itself becomes the game file — exactly the shape a plain
        /// folder-release import already produces.
        ///
        /// When false (the original behaviour) <see cref="EntryName"/> names the
        /// one ROM/disc image to pull out, and that file is the game file.
        /// </summary>
        public bool ExtractWholeArchive { get; set; }

        public override string ToString()
        {
            return ExtractWholeArchive ? $"{ArchivePath}!*" : $"{ArchivePath}!{EntryName}";
        }
    }
}
