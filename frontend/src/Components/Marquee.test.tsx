import { act, render } from '@testing-library/react';
import Marquee from './Marquee';

import '@testing-library/jest-dom';

// Marquee measures its container and its text node via useMeasure (a
// ResizeObserver-backed hook). jsdom doesn't lay elements out, so drive the
// widths directly: the hook is called once for the container and once for
// the text node, in that fixed order, every render.
jest.mock('Helpers/Hooks/useMeasure', () => ({
  __esModule: true,
  default: jest.fn(),
}));

import useMeasure from 'Helpers/Hooks/useMeasure';

const mockedUseMeasure = useMeasure as jest.Mock;

let callCount = 0;
let containerWidth = 0;
let textWidth = 0;

function bounds(width: number) {
  return {
    width,
    height: 0,
    top: 0,
    left: 0,
    bottom: 0,
    right: 0,
    x: 0,
    y: 0,
  };
}

function noopRef() {
  return undefined;
}

describe('Marquee', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    callCount = 0;
    containerWidth = 0;
    textWidth = 0;

    mockedUseMeasure.mockImplementation(() => {
      const isContainerCall = callCount % 2 === 0;
      callCount += 1;

      return [noopRef, bounds(isContainerCall ? containerWidth : textWidth)];
    });
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('should not animate when the text fits its container', () => {
    containerWidth = 200;
    textWidth = 100;

    const { container } = render(<Marquee text="Short Title" />);
    const span = container.querySelector('span');

    act(() => {
      jest.advanceTimersByTime(1000);
    });

    expect(span).toHaveStyle({ right: '0px' });
  });

  it('should animate when the text overflows its container', () => {
    containerWidth = 100;
    textWidth = 300;

    const { container } = render(<Marquee text="A Very Long Game Title" />);
    const span = container.querySelector('span');

    act(() => {
      jest.advanceTimersByTime(500);
    });

    expect(span?.style.right).not.toBe('0px');
  });

  it('should reset the offset back to 0 when a re-measure removes the overflow', () => {
    containerWidth = 100;
    textWidth = 300;

    const { container, rerender } = render(
      <Marquee text="A Very Long Game Title" />
    );
    const span = container.querySelector('span');

    act(() => {
      jest.advanceTimersByTime(500);
    });
    expect(span?.style.right).not.toBe('0px');

    // The container grows (eg. a layout-driven width prop resolves) so the
    // text no longer overflows. Re-measuring should snap the offset back to
    // 0 instead of leaving the animation mid-scroll.
    containerWidth = 400;
    act(() => {
      rerender(<Marquee text="A Very Long Game Title" />);
    });

    expect(span).toHaveStyle({ right: '0px' });
  });
});
