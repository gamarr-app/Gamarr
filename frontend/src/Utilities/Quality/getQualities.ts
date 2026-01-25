import Quality from 'Quality/Quality';
import { QualityProfileQualityItem } from 'typings/QualityProfile';

export default function getQualities(
  qualities: QualityProfileQualityItem[] | undefined
): Quality[] {
  if (!qualities) {
    return [];
  }

  return qualities.reduce((acc: Quality[], item) => {
    if (item.quality) {
      acc.push(item.quality);
    } else {
      const groupQualities = item.items
        .filter((i) => i.quality)
        .map((i) => i.quality as Quality);
      acc.push(...groupQualities);
    }

    return acc;
  }, []);
}
