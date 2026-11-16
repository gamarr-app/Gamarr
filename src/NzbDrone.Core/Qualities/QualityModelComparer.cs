using System.Collections.Generic;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Qualities
{
    public class QualityModelComparer : IComparer<Quality>, IComparer<QualityModel>
    {
        private readonly QualityProfile _profile;

        public QualityModelComparer(QualityProfile profile)
        {
            Ensure.That(profile, () => profile).IsNotNull();
            Ensure.That(profile.Items, () => profile.Items).HasItems();

            _profile = profile;
        }

        public int Compare(int left, int right, bool respectGroupOrder = false)
        {
            var leftIndex = _profile.GetIndex(left);
            var rightIndex = _profile.GetIndex(right);

            return leftIndex.CompareTo(rightIndex, respectGroupOrder);
        }

        public int Compare(Quality x, Quality y)
        {
            return Compare(x, y, false);
        }

        public int Compare(Quality left, Quality right, bool respectGroupOrder)
        {
            var leftIndex = _profile.GetIndex(left, respectGroupOrder);
            var rightIndex = _profile.GetIndex(right, respectGroupOrder);

            return leftIndex.CompareTo(rightIndex, respectGroupOrder);
        }

        public int Compare(QualityModel x, QualityModel y)
        {
            return Compare(x, y, false);
        }

        public int Compare(QualityModel left, QualityModel right, bool respectGroupOrder)
        {
            var result = Compare(left.Quality, right.Quality, respectGroupOrder);

            if (result == 0)
            {
                result = left.Revision.CompareTo(right.Revision);
            }

            return result;
        }
    }
}
