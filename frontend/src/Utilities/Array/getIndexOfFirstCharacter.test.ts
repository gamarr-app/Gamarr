import getIndexOfFirstCharacter from './getIndexOfFirstCharacter';

describe('getIndexOfFirstCharacter', () => {
  const items = [
    { sortTitle: '123 numbers' },
    { sortTitle: 'alpha' },
    { sortTitle: 'beta' },
    { sortTitle: 'charlie' },
  ];

  it('should find index of first item starting with a letter', () => {
    expect(getIndexOfFirstCharacter(items, 'a')).toBe(1);
  });

  it('should find index of first item starting with # (number)', () => {
    expect(getIndexOfFirstCharacter(items, '#')).toBe(0);
  });

  it('should return -1 when character not found', () => {
    expect(getIndexOfFirstCharacter(items, 'z')).toBe(-1);
  });

  it('should return -1 for empty array', () => {
    expect(getIndexOfFirstCharacter([], 'a')).toBe(-1);
  });

  it('should find correct character in middle of list', () => {
    expect(getIndexOfFirstCharacter(items, 'c')).toBe(3);
  });

  it('should return -1 for # when no numeric items', () => {
    const alphaOnly = [{ sortTitle: 'alpha' }, { sortTitle: 'beta' }];
    expect(getIndexOfFirstCharacter(alphaOnly, '#')).toBe(-1);
  });
});
