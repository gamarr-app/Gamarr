import { GameIndexItem } from 'Store/Selectors/createGameClientSideCollectionItemsSelector';

export interface GroupedGameIndexItem extends GameIndexItem {
  // Present (with 2+ entries, primary included) only when multiple
  // per-platform entries of the same title collapse into one card.
  platformSiblingIds?: number[];
}

function identityKey(item: GameIndexItem): string {
  if (item.steamAppId) {
    return `steam:${item.steamAppId}`;
  }

  if (item.igdbId) {
    return `igdb:${item.igdbId}`;
  }

  return `title:${item.sortTitle}:${item.year ?? 0}`;
}

// Collapses per-platform entries of the same title (#150) into the entry
// that sorts first; sort order between groups is untouched.
export default function groupGameIndexItems(
  items: GameIndexItem[]
): GroupedGameIndexItem[] {
  const byKey = new Map<string, GroupedGameIndexItem>();
  const result: GroupedGameIndexItem[] = [];

  items.forEach((item) => {
    const key = identityKey(item);
    const existing = byKey.get(key);

    if (existing) {
      existing.platformSiblingIds = [
        ...(existing.platformSiblingIds ?? [existing.id]),
        item.id,
      ];
    } else {
      const grouped: GroupedGameIndexItem = { ...item };
      byKey.set(key, grouped);
      result.push(grouped);
    }
  });

  return result;
}
