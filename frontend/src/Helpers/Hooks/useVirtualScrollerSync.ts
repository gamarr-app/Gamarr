import { throttle } from 'lodash';
import { RefObject, useEffect } from 'react';

/**
 * The window's scroll offset, read defensively because the scrolling element
 * differs between standards and quirks mode.
 */
export function getWindowScrollTopPosition() {
  return document.documentElement.scrollTop || document.body.scrollTop || 0;
}

/**
 * The subset of react-window's `ListImperativeAPI` / `GridImperativeAPI` that
 * this hook needs, so a ref to either can be passed in.
 */
interface VirtualizedElementApi {
  element: HTMLElement | null;
}

interface UseVirtualScrollerSyncOptions {
  /**
   * The page-level scrolling element that the virtualized list is rendered
   * inside of. On small screens the window scrolls instead, but this element is
   * still used to work out the list's offset within the page.
   */
  scrollerRef: RefObject<HTMLElement | null>;

  /**
   * Ref to the react-window `List` or `Grid` imperative API.
   */
  virtualizedRef: RefObject<VirtualizedElementApi | null>;

  /**
   * When true the window is the scrolling element rather than `scrollerRef`.
   */
  isSmallScreen?: boolean;
}

/**
 * react-window renders a list that is only as tall as its own viewport, but
 * Gamarr's pages scroll the whole page (or the page content body) rather than
 * the list itself. This hook forwards the page's scroll offset into the
 * virtualized element so it renders the rows the user is actually looking at.
 *
 * This replaces react-virtualized's `WindowScroller`, which was removed in
 * react-virtualized 10 and has no equivalent in react-window.
 */
interface ScrollToRowStartOptions {
  scrollerRef: RefObject<HTMLElement | null>;
  virtualizedRef: RefObject<VirtualizedElementApi | null>;
  rowIndex: number;
  rowHeight: number;

  /**
   * Pixels to add once the target is past the first row, to account for
   * content rendered above the virtualized rows inside the same scroller.
   */
  offset?: number;
}

/**
 * Scroll so the given row sits at the top of the viewport.
 *
 * This preserves react-virtualized's `scrollToAlignment="start"`, which its
 * `getOffsetForCell()` used as the default alignment. react-window's own
 * `scrollToRow` scrolls the list's internal element, which this component's
 * scroll sync would immediately overwrite, so the page scroller is moved
 * instead and the list element is updated to match.
 */
export function scrollToRowStart({
  scrollerRef,
  virtualizedRef,
  rowIndex,
  rowHeight,
  offset = 0,
}: ScrollToRowStartOptions) {
  let scrollTop = rowIndex * rowHeight;

  if (scrollTop > 0) {
    scrollTop += offset;
  }

  const element = virtualizedRef.current?.element;

  if (element) {
    element.scrollTop = scrollTop;
  }

  scrollerRef.current?.scrollTo(0, scrollTop);
}

function useVirtualScrollerSync({
  scrollerRef,
  virtualizedRef,
  isSmallScreen = false,
}: UseVirtualScrollerSyncOptions) {
  useEffect(() => {
    const scrollListener = isSmallScreen ? window : scrollerRef.current;

    if (!scrollListener) {
      return;
    }

    const handleScroll = throttle(() => {
      const scroller = scrollerRef.current;

      if (!scroller) {
        return;
      }

      const { offsetTop = 0 } = scroller;
      const scrollTop =
        (isSmallScreen ? getWindowScrollTopPosition() : scroller.scrollTop) -
        offsetTop;

      const element = virtualizedRef.current?.element;

      if (element) {
        element.scrollTop = scrollTop;
      }
    }, 10);

    scrollListener.addEventListener('scroll', handleScroll);

    return () => {
      handleScroll.cancel();
      scrollListener.removeEventListener('scroll', handleScroll);
    };
  }, [isSmallScreen, scrollerRef, virtualizedRef]);
}

export default useVirtualScrollerSync;
