import { Data } from 'popper.js';
import { CSSProperties } from 'react';

// Popper.js computes right and bottom at runtime from left+width and top+height
// but the official types don't include them. This type extends Data with
// the computed properties for use in modifier functions.
export interface PopperModifierData extends Omit<Data, 'offsets' | 'styles'> {
  offsets: {
    popper: {
      top: number;
      left: number;
      width: number;
      height: number;
      right: number;
      bottom: number;
    };
    reference: {
      top: number;
      left: number;
      width: number;
      height: number;
      right: number;
      bottom: number;
    };
    arrow: {
      top: number;
      left: number;
    };
  };
  styles: CSSProperties & {
    maxHeight?: number;
    maxWidth?: number;
  };
}

// Type assertion helper for Popper modifier functions
export function asPopperModifier(
  fn: (data: PopperModifierData) => PopperModifierData
): (data: Data, options: object) => Data {
  return fn as unknown as (data: Data, options: object) => Data;
}
