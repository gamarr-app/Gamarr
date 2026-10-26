/**
 * Identity helpers for comparing a game against the library.
 *
 * Gamarr's primary metadata source is Steam, and a Steam-sourced search result
 * never carries an IGDB id (`SteamStoreProxy.MapSearchItem` leaves `IgdbId` at
 * its default), so `igdbId` is `0` on every lookup row unless IGDB credentials
 * are configured *and* the titles match. Comparing games on `igdbId` alone
 * therefore makes every unidentified game look like every other unidentified
 * game. Always go through these helpers instead.
 */

export interface GameIdentity {
  steamAppId?: number;
  igdbId?: number;
}

/**
 * True when the game carries an id we can actually compare on. A game with
 * neither id is "unidentified" and must never be treated as equal to anything.
 */
export function hasGameIdentity(game?: GameIdentity | null): boolean {
  if (!game) {
    return false;
  }

  return (
    (!!game.steamAppId && game.steamAppId > 0) ||
    (!!game.igdbId && game.igdbId > 0)
  );
}

/**
 * True when both games carry the same id. Steam app id is the primary
 * identifier; IGDB id is only consulted when neither side has a Steam app id,
 * so a Steam match is never second-guessed by a zeroed IGDB id.
 */
export function isSameGame(a?: GameIdentity | null, b?: GameIdentity | null) {
  if (!a || !b) {
    return false;
  }

  if (a.steamAppId && a.steamAppId > 0 && b.steamAppId && b.steamAppId > 0) {
    return a.steamAppId === b.steamAppId;
  }

  if (a.igdbId && a.igdbId > 0 && b.igdbId && b.igdbId > 0) {
    return a.igdbId === b.igdbId;
  }

  return false;
}

/**
 * True when `game` is already in the library. An unidentified game is never
 * considered existing — otherwise the Library Import page disables every row.
 */
export default function isExistingGame(
  games: GameIdentity[],
  game?: GameIdentity | null
): boolean {
  if (!hasGameIdentity(game)) {
    return false;
  }

  return games.some((libraryGame) => isSameGame(libraryGame, game));
}
