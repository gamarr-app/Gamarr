import 'redux-actions';
import 'redux-batched-actions';

// Bridge Redux 5's stricter types with redux-actions' Action<T> and
// redux-batched-actions' BatchAction types. Redux 5 requires UnknownAction
// (with [key: string]: unknown index signature) but these types don't have one.
declare module 'redux-actions' {
  interface BaseAction {
    [key: string]: unknown;
  }
}

declare module 'redux-batched-actions' {
  interface BatchAction {
    [key: string]: unknown;
  }
}
