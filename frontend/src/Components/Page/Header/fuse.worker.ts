import Fuse from 'fuse.js';
import type {
  FuseWorkerRequest,
  FuseWorkerResponse,
  GameSuggestion,
  SuggestedGame,
} from './GameSearchInput';

const fuseOptions = {
  shouldSort: true,
  includeMatches: true,
  ignoreLocation: true,
  threshold: 0.3,
  minMatchCharLength: 1,
  keys: ['title', 'alternateTitles.title', 'igdbId', 'tags.label'],
};

function getSuggestions(games: SuggestedGame[], value: string) {
  const limit = 10;
  let suggestions = [];

  if (value.length === 1) {
    for (let i = 0; i < games.length; i++) {
      const m = games[i];
      if (m.firstCharacter === value.toLowerCase()) {
        suggestions.push({
          item: games[i],
          indices: [[0, 0]],
          matches: [
            {
              value: m.title,
              key: 'title',
            },
          ],
          refIndex: 0,
        });
        if (suggestions.length > limit) {
          break;
        }
      }
    }
  } else {
    const fuse = new Fuse(games, fuseOptions);
    suggestions = fuse.search(value, { limit });
  }

  // Fuse results and the single-character shortcut both expose the fields the
  // UI reads (item, matches); cast to the consumer contract at the boundary.
  return suggestions as unknown as GameSuggestion[];
}

onmessage = function (e: MessageEvent<FuseWorkerRequest>) {
  if (!e) {
    return;
  }

  const { games, value } = e.data;

  const suggestions = getSuggestions(games, value);

  const results: FuseWorkerResponse = {
    value,
    suggestions,
  };

  self.postMessage(results);
};
