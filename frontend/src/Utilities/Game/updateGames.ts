import _ from 'lodash';
import Game from 'Game/Game';
import { update } from 'Store/Actions/baseActions';

interface UpdateOptions {
  [key: string]: unknown;
}

function updateGames(
  section: string,
  games: Game[],
  gameIds: number[],
  options: UpdateOptions
) {
  const data = _.reduce(
    games,
    (result: Game[], item) => {
      if (gameIds.indexOf(item.id) > -1) {
        result.push({
          ...item,
          ...options,
        } as Game);
      } else {
        result.push(item);
      }

      return result;
    },
    []
  );

  return update({ section, data });
}

export default updateGames;
