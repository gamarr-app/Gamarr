import { connectRouter } from 'connected-react-router';
import { History } from 'history';
import { combineReducers, Reducer } from 'redux';
import { enableBatching } from 'redux-batched-actions';
import actions from 'Store/Actions';

interface ActionModule {
  section: string;
  defaultState: unknown;
  reducers: Reducer;
}

const defaultState: Record<string, unknown> = {};
const reducers: Record<string, Reducer> = {};

(actions as unknown as ActionModule[]).forEach((action) => {
  const section = action.section;

  defaultState[section] = action.defaultState;
  reducers[section] = action.reducers;
});

export { defaultState };

export default function (history: History): Reducer {
  return enableBatching(
    combineReducers({
      ...reducers,
      router: connectRouter(history),
    })
  );
}
