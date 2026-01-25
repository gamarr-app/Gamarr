declare module 'react-measure' {
  import React from 'react';

  // Old API (v1.x) - used in some parts of this codebase
  type WhitelistOption =
    | 'width'
    | 'height'
    | 'top'
    | 'bottom'
    | 'left'
    | 'right';

  // Dimensions object returned by onMeasure (old API)
  interface MeasureDimensions {
    width?: number;
    height?: number;
    top?: number;
    bottom?: number;
    left?: number;
    right?: number;
  }

  // New API (v2.x) - used in some parts of this codebase
  export interface ContentRect {
    bounds?: {
      width: number;
      height: number;
      top: number;
      right: number;
      bottom: number;
      left: number;
    };
    scroll?: {
      width: number;
      height: number;
      top: number;
      left: number;
    };
    offset?: {
      width: number;
      height: number;
      top: number;
      left: number;
    };
    margin?: {
      top: number;
      right: number;
      bottom: number;
      left: number;
    };
    client?: {
      width: number;
      height: number;
      top: number;
      left: number;
    };
    entry?: ResizeObserverEntry;
  }

  export interface MeasuredComponentProps {
    measureRef: React.Ref<Element>;
    measure: () => void;
    contentRect: ContentRect;
  }

  export interface MeasureProps {
    // Old API props
    whitelist?: WhitelistOption[];
    blacklist?: WhitelistOption[];
    includeMargin?: boolean;
    useClone?: boolean;
    cloneOptions?: {
      remove?: boolean;
      display?: string;
      width?: number;
      height?: number | string;
    };
    shouldMeasure?: boolean;
    onMeasure?: (dimensions: MeasureDimensions) => void;

    // New API props
    bounds?: boolean;
    scroll?: boolean;
    offset?: boolean;
    margin?: boolean;
    client?: boolean;
    innerRef?: React.Ref<Element>;
    onResize?: (contentRect: ContentRect) => void;

    // Children can be either JSX (old API) or render prop (new API)
    children?:
      | React.ReactNode
      | ((props: MeasuredComponentProps) => React.ReactNode);
  }

  export default class Measure extends React.Component<MeasureProps> {}

  // Re-export for backward compatibility
  export { MeasureProps };
}
