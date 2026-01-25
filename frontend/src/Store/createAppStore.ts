import { History } from 'history';
import { createStore, Store } from 'redux';
import createReducers, { defaultState } from 'Store/Actions/createReducers';
import middlewares from 'Store/Middleware/middlewares';

function createAppStore(history: History): Store {
  const appStore = createStore(
    createReducers(history),
    defaultState,
    middlewares(history)
  );

  return appStore;
}

export default createAppStore;
