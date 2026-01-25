import { AnyAction } from 'redux';
import { ThunkAction, ThunkDispatch } from 'redux-thunk';
import AppState from 'App/State/AppState';

export type GetState = () => AppState;
export type AppThunk<ReturnType = unknown> = ThunkAction<
  ReturnType,
  AppState,
  undefined,
  AnyAction
>;
export type AppDispatch = ThunkDispatch<AppState, undefined, AnyAction>;
type Thunk = (
  getState: GetState,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  identityFn: any,
  dispatch: AppDispatch
) => unknown;

const thunks: Record<string, Thunk> = {};

function identity<T>(payload: T): T {
  return payload;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type IdentityFunction = (payload: any) => any;

export function createThunk(
  type: string,
  identityFunction: IdentityFunction = identity
) {
  return function <T>(payload?: T): AppThunk {
    return function (dispatch: AppDispatch, getState: GetState) {
      const thunk = thunks[type];

      if (thunk) {
        const finalPayload = payload ?? {};

        return thunk(getState, identityFunction(finalPayload), dispatch);
      }

      throw Error(`Thunk handler has not been registered for ${type}`);
    };
  };
}

export function handleThunks(handlers: Record<string, Thunk>) {
  const types = Object.keys(handlers);

  types.forEach((type) => {
    thunks[type] = handlers[type];
  });
}
