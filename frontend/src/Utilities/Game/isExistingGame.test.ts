import isExistingGame, { hasGameIdentity, isSameGame } from './isExistingGame';

describe('hasGameIdentity', () => {
  it('should be false for a missing game', () => {
    expect(hasGameIdentity(undefined)).toBe(false);
    expect(hasGameIdentity(null)).toBe(false);
  });

  it('should be false when both ids are zero', () => {
    expect(hasGameIdentity({ steamAppId: 0, igdbId: 0 })).toBe(false);
  });

  it('should be false when both ids are absent', () => {
    expect(hasGameIdentity({})).toBe(false);
  });

  it('should be true for a steam app id', () => {
    expect(hasGameIdentity({ steamAppId: 440, igdbId: 0 })).toBe(true);
  });

  it('should be true for an igdb id', () => {
    expect(hasGameIdentity({ steamAppId: 0, igdbId: 1234 })).toBe(true);
  });
});

describe('isSameGame', () => {
  it('should match on steam app id', () => {
    expect(
      isSameGame({ steamAppId: 440, igdbId: 0 }, { steamAppId: 440, igdbId: 0 })
    ).toBe(true);
  });

  it('should not match different steam app ids that share a zero igdb id', () => {
    expect(
      isSameGame({ steamAppId: 440, igdbId: 0 }, { steamAppId: 570, igdbId: 0 })
    ).toBe(false);
  });

  it('should fall back to igdb id when steam app ids are missing', () => {
    expect(isSameGame({ igdbId: 1234 }, { igdbId: 1234 })).toBe(true);
    expect(isSameGame({ igdbId: 1234 }, { igdbId: 5678 })).toBe(false);
  });

  it('should never match two unidentified games', () => {
    expect(
      isSameGame({ steamAppId: 0, igdbId: 0 }, { steamAppId: 0, igdbId: 0 })
    ).toBe(false);
  });

  it('should be false when either side is missing', () => {
    expect(isSameGame(undefined, { steamAppId: 440 })).toBe(false);
    expect(isSameGame({ steamAppId: 440 }, undefined)).toBe(false);
  });
});

describe('isExistingGame', () => {
  // Every Steam-sourced lookup result carries igdbId 0, and so does every
  // Steam-added library game. Matching on igdbId alone made every import row
  // look like it was already in the library, which disabled every checkbox.
  const library = [
    { steamAppId: 440, igdbId: 0 },
    { steamAppId: 570, igdbId: 0 },
  ];

  it('should not treat an unmatched steam result as existing just because igdb ids are both zero', () => {
    expect(isExistingGame(library, { steamAppId: 730, igdbId: 0 })).toBe(false);
  });

  it('should treat a matching steam app id as existing', () => {
    expect(isExistingGame(library, { steamAppId: 440, igdbId: 0 })).toBe(true);
  });

  it('should be false for a game with no usable id', () => {
    expect(isExistingGame(library, { steamAppId: 0, igdbId: 0 })).toBe(false);
  });

  it('should be false when no game is given', () => {
    expect(isExistingGame(library, undefined)).toBe(false);
  });

  it('should be false against an empty library', () => {
    expect(isExistingGame([], { steamAppId: 440, igdbId: 0 })).toBe(false);
  });

  it('should match on igdb id when neither side has a steam app id', () => {
    expect(isExistingGame([{ igdbId: 1234 }], { igdbId: 1234 })).toBe(true);
    expect(isExistingGame([{ igdbId: 1234 }], { igdbId: 5678 })).toBe(false);
  });
});
