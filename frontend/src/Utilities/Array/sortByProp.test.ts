import sortByProp from './sortByProp';

describe('sortByProp', () => {
  it('should sort objects by string property', () => {
    const items = [{ name: 'Charlie' }, { name: 'Alice' }, { name: 'Bob' }];

    const sorted = [...items].sort(sortByProp('name'));
    expect(sorted.map((i) => i.name)).toEqual(['Alice', 'Bob', 'Charlie']);
  });

  it('should sort numerically within strings', () => {
    const items = [
      { title: 'Item 10' },
      { title: 'Item 2' },
      { title: 'Item 1' },
    ];

    const sorted = [...items].sort(sortByProp('title'));
    expect(sorted.map((i) => i.title)).toEqual(['Item 1', 'Item 2', 'Item 10']);
  });

  it('should handle equal values', () => {
    const items = [{ name: 'Alice' }, { name: 'Alice' }];

    const sorted = [...items].sort(sortByProp('name'));
    expect(sorted.map((i) => i.name)).toEqual(['Alice', 'Alice']);
  });

  it('should handle single item array', () => {
    const items = [{ name: 'Alice' }];
    const sorted = [...items].sort(sortByProp('name'));
    expect(sorted).toEqual([{ name: 'Alice' }]);
  });

  it('should handle empty array', () => {
    const items: { name: string }[] = [];
    const sorted = [...items].sort(sortByProp('name'));
    expect(sorted).toEqual([]);
  });

  it('should be case-sensitive in sorting', () => {
    const items = [{ name: 'banana' }, { name: 'Apple' }, { name: 'cherry' }];

    const sorted = [...items].sort(sortByProp('name'));
    expect(sorted[0].name).toBe('Apple');
  });
});
