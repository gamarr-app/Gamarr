import { AppDispatch } from 'Store/thunks';

declare module 'react-redux' {
  // Override useDispatch to return AppDispatch (ThunkDispatch) so that
  // dispatch(thunk()) calls type-check correctly with Redux 5.
  // Preserves withTypes for creating additional typed hooks.
  interface UseDispatch {
    (): AppDispatch;
    withTypes<T>(): () => T;
  }
  export const useDispatch: UseDispatch;
}
