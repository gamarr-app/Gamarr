import _ from 'lodash';
import { update } from 'Store/Actions/baseActions';

function updateGames(section, games, gameIds, options) {
  const data = _.reduce(games, (result, item) => {
    if (gameIds.indexOf(item.id) > -1) {
      result.push({
        ...item,
        ...options
      });
    } else {
      result.push(item);
    }

    return result;
  }, []);

  return update({ section, data });
}

export default updateGames;
