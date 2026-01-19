import _ from 'lodash';

export default function migrateMonitorToEnum(persistedState) {
  const addGame = _.get(persistedState, 'addGame.defaults.monitor');
  const discoverGame = _.get(persistedState, 'discoverGame.defaults.monitor');

  if (addGame) {
    if (addGame === 'true') {
      persistedState.addGame.defaults.monitor = 'gameOnly';
    }

    if (addGame === 'false') {
      persistedState.addGame.defaults.monitor = 'none';
    }
  }

  if (discoverGame) {
    if (discoverGame === 'true') {
      persistedState.discoverGame.defaults.monitor = 'gameOnly';
    }

    if (discoverGame === 'false') {
      persistedState.discoverGame.defaults.monitor = 'none';
    }
  }
}
