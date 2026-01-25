/* eslint-disable init-declarations */
declare module 'react-dnd-multi-backend' {
  import React from 'react';
  import { DndProviderProps } from 'react-dnd';

  export interface DndProviderMultiBackendProps
    extends Omit<DndProviderProps<unknown>, 'backend'> {
    options: unknown;
  }

  export const DndProvider: React.ComponentType<DndProviderMultiBackendProps>;
}

declare module 'react-dnd-multi-backend/dist/esm/HTML5toTouch' {
  const HTML5toTouch: unknown;
  export default HTML5toTouch;
}
/* eslint-enable init-declarations */
