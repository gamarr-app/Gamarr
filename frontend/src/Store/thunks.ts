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

export type Thunk<T = unknown> = (
  getState: GetState,
  payload: T,
  dispatch: AppDispatch
) => unknown;

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- Thunks registry holds handlers with varying payload types
const thunks: Record<string, Thunk<any>> = {};

function identity<T>(payload: T): T {
  return payload;
}

export function createThunk<TInput = unknown, TOutput = TInput>(
  type: string,
  identityFunction: (payload: TInput) => TOutput = identity as unknown as (
    payload: TInput
  ) => TOutput
) {
  return function (payload?: TInput): AppThunk {
    return function (dispatch: AppDispatch, getState: GetState) {
      const thunk = thunks[type];

      if (thunk) {
        const finalPayload = (payload ?? {}) as TInput;

        return thunk(getState, identityFunction(finalPayload), dispatch);
      }

      throw Error(`Thunk handler has not been registered for ${type}`);
    };
  };
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- Handlers have varying payload types
export function handleThunks(handlers: Record<string, Thunk<any>>) {
  const types = Object.keys(handlers);

  types.forEach((type) => {
    thunks[type] = handlers[type];
  });
}
