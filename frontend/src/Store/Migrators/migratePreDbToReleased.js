import _ from 'lodash';

export default function migratePreDbToReleased(persistedState) {
  const addGame = _.get(persistedState, 'addGame.defaults.minimumAvailability');
  const discoverGame = _.get(persistedState, 'discoverGame.defaults.minimumAvailability');

  if (!addGame && !discoverGame) {
    return;
  }

  if (addGame === 'preDB') {
    persistedState.addGame.defaults.minimumAvailability = 'released';
  }

  if (discoverGame === 'preDB') {
    persistedState.discoverGame.defaults.minimumAvailability = 'released';
  }
}
