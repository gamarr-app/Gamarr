import classNames from 'classnames';
import { CSSProperties, useCallback, useEffect, useRef, useState } from 'react';
import useMeasure from 'Helpers/Hooks/useMeasure';

const FPS = 20;
const STEP = 1;
const TIMEOUT = (1 / FPS) * 1000;

interface MarqueeProps {
  text?: string;
  title?: string;
  hoverToStop?: boolean;
  loop?: boolean;
  className?: string;
}

function Marquee(props: MarqueeProps) {
  const {
    text = '',
    title = '',
    hoverToStop = true,
    loop = false,
    className = '',
  } = props;

  const [containerRef, { width: containerWidth }] = useMeasure();
  const [textRef, { width: textWidth }] = useMeasure();
  const marqueeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // The step loop reads/writes these every frame. Keeping them in refs
  // (instead of state) means the loop never needs to be rebuilt mid-flight.
  const animatedWidthRef = useRef(0);
  const directionRef = useRef(0);

  const startRef = useRef<() => void>(() => undefined);
  const stopRef = useRef<() => void>(() => undefined);

  const [animatedWidth, setAnimatedWidth] = useState(0);
  const [overflowWidth, setOverflowWidth] = useState(0);

  // Always restart the scroll from the beginning when the text itself
  // changes, independent of whether that happens to change the overflow.
  useEffect(() => {
    animatedWidthRef.current = 0;
    directionRef.current = 0;
    setAnimatedWidth(0);
  }, [text]);

  // Re-measure whenever the container or the text node is resized, not just
  // when the text changes. The container's width is frequently set from
  // layout computed after mount (eg. GameDetails sizes it from a measured
  // sibling), so a one-shot measurement keyed only on text can run against
  // the box's pre-layout width and never get corrected. containerWidth and
  // textWidth come from ResizeObserver via useMeasure, so this effect reruns
  // whenever either box actually changes size.
  useEffect(() => {
    if (!containerWidth || !textWidth) {
      return;
    }

    const newOverflowWidth = textWidth - containerWidth;

    setOverflowWidth((prevOverflowWidth) => {
      if (prevOverflowWidth === newOverflowWidth) {
        return prevOverflowWidth;
      }

      // The box resized: snap back to the start instead of leaving the
      // animation mid-scroll (eg. a title that now fits should rest at
      // right: 0, not keep scrolling from wherever it was).
      animatedWidthRef.current = 0;
      directionRef.current = 0;
      setAnimatedWidth(0);

      return newOverflowWidth;
    });
  }, [containerWidth, textWidth]);

  // Single effect owns the animation loop. It only depends on primitive
  // values that should actually restart it, so it settles once mounted
  // instead of re-running (and re-timing) on every animated frame.
  useEffect(() => {
    const stop = () => {
      if (marqueeTimerRef.current) {
        clearTimeout(marqueeTimerRef.current);
        marqueeTimerRef.current = null;
      }
    };

    stopRef.current = stop;

    // Nothing to animate: the title fits its box. Keep it pinned at 0 and
    // don't start a timer at all (this is the jitter guard).
    if (overflowWidth <= 0) {
      animatedWidthRef.current = 0;
      directionRef.current = 0;
      setAnimatedWidth(0);
      startRef.current = () => undefined;

      return stop;
    }

    const animate = () => {
      const steppedWidth =
        directionRef.current === 0
          ? animatedWidthRef.current + STEP
          : animatedWidthRef.current - STEP;

      const isRoundOver = steppedWidth < 0;

      if (isRoundOver) {
        if (loop) {
          directionRef.current = directionRef.current === 0 ? 1 : 0;
          animatedWidthRef.current = 0;
        } else {
          stop();
          return;
        }
      } else {
        if (steppedWidth > overflowWidth) {
          directionRef.current = directionRef.current === 1 ? 0 : 1;
        }

        animatedWidthRef.current = steppedWidth;
      }

      setAnimatedWidth(animatedWidthRef.current);
      marqueeTimerRef.current = setTimeout(animate, TIMEOUT);
    };

    const start = () => {
      stop();
      const isLeading = animatedWidthRef.current === 0;
      marqueeTimerRef.current = setTimeout(animate, isLeading ? 0 : TIMEOUT);
    };

    startRef.current = start;

    if (hoverToStop) {
      start();
    }

    return stop;
  }, [overflowWidth, hoverToStop, loop]);

  const onHandleMouseEnter = useCallback(() => {
    if (hoverToStop) {
      stopRef.current();
    } else if (overflowWidth > 0) {
      startRef.current();
    }
  }, [hoverToStop, overflowWidth]);

  const onHandleMouseLeave = useCallback(() => {
    if (hoverToStop && overflowWidth > 0) {
      startRef.current();
    } else {
      stopRef.current();
      animatedWidthRef.current = 0;
      directionRef.current = 0;
      setAnimatedWidth(0);
    }
  }, [hoverToStop, overflowWidth]);

  const style: CSSProperties = {
    position: 'relative',
    right: animatedWidth,
    whiteSpace: 'nowrap',
  };

  const titleText = title && text !== title ? `Original Title: ${title}` : text;

  if (overflowWidth < 0) {
    return (
      <div
        ref={containerRef}
        className={classNames('ui-marquee', className)}
        style={{ overflow: 'hidden' }}
      >
        <span ref={textRef} style={style} title={titleText}>
          {text}
        </span>
      </div>
    );
  }

  return (
    <div
      ref={containerRef}
      className={classNames('ui-marquee', className)}
      style={{ overflow: 'hidden' }}
      onMouseEnter={onHandleMouseEnter}
      onMouseLeave={onHandleMouseLeave}
    >
      <span ref={textRef} style={style} title={titleText}>
        {text}
      </span>
    </div>
  );
}

export default Marquee;
