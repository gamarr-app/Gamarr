import { GameIndexItem } from 'Store/Selectors/createGameClientSideCollectionItemsSelector';
import groupGameIndexItems from './groupGameIndexItems';

function item(
  overrides: Partial<GameIndexItem> & { id: number }
): GameIndexItem {
  return {
    sortTitle: `game ${overrides.id}`,
    ...overrides,
  };
}

describe('groupGameIndexItems', () => {
  it('should keep ungrouped items untouched and in order', () => {
    const items = [
      item({ id: 1, steamAppId: 100 }),
      item({ id: 2, steamAppId: 200 }),
    ];

    const result = groupGameIndexItems(items);

    expect(result.map((r) => r.id)).toEqual([1, 2]);
    expect(result[0].platformSiblingIds).toBeUndefined();
  });

  it('should group same steam id entries under the first and include all ids', () => {
    const items = [
      item({ id: 1, steamAppId: 100, platform: 'windows' }),
      item({ id: 2, steamAppId: 200 }),
      item({ id: 3, steamAppId: 100, platform: 'nintendoSwitch' }),
    ];

    const result = groupGameIndexItems(items);

    expect(result.map((r) => r.id)).toEqual([1, 2]);
    expect(result[0].platformSiblingIds).toEqual([1, 3]);
  });

  it('should group by igdb id when steam id is missing', () => {
    const items = [
      item({ id: 1, igdbId: 55 }),
      item({ id: 2, igdbId: 55 }),
      item({ id: 3, igdbId: 56 }),
    ];

    const result = groupGameIndexItems(items);

    expect(result.map((r) => r.id)).toEqual([1, 3]);
    expect(result[0].platformSiblingIds).toEqual([1, 2]);
  });

  it('should fall back to title and year identity', () => {
    const items = [
      item({ id: 1, sortTitle: 'hades', year: 2020 }),
      item({ id: 2, sortTitle: 'hades', year: 2020 }),
      item({ id: 3, sortTitle: 'hades ii', year: 2024 }),
    ];

    const result = groupGameIndexItems(items);

    expect(result.map((r) => r.id)).toEqual([1, 3]);
    expect(result[0].platformSiblingIds).toEqual([1, 2]);
  });

  it('should not mix steam and title identities', () => {
    const items = [
      item({ id: 1, steamAppId: 100, sortTitle: 'same' }),
      item({ id: 2, sortTitle: 'same' }),
    ];

    const result = groupGameIndexItems(items);

    expect(result).toHaveLength(2);
  });

  it('should group three or more entries', () => {
    const items = [
      item({ id: 1, steamAppId: 100 }),
      item({ id: 2, steamAppId: 100 }),
      item({ id: 3, steamAppId: 100 }),
    ];

    const result = groupGameIndexItems(items);

    expect(result).toHaveLength(1);
    expect(result[0].platformSiblingIds).toEqual([1, 2, 3]);
  });
});
