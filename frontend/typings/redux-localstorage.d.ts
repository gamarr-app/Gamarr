declare module 'redux-localstorage' {
  import { StoreEnhancer } from 'redux';

  interface PersistStateConfig<T = unknown> {
    key?: string;
    slicer?: (paths: string[]) => (state: T) => T;
    serialize?: (obj: T) => string;
    deserialize?: (str: string) => T;
    merge?: (initialState: T, persistedState: T | null) => T;
  }

  function persistState<T = unknown>(
    paths?: string[],
    config?: PersistStateConfig<T>
  ): StoreEnhancer;

  export default persistState;
}
