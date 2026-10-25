import classNames from 'classnames';
import { CSSProperties, useCallback, useEffect, useRef, useState } from 'react';

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

  const containerRef = useRef<HTMLDivElement | null>(null);
  const textRef = useRef<HTMLSpanElement | null>(null);
  const marqueeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // The step loop reads/writes these every frame. Keeping them in refs
  // (instead of state) means the loop never needs to be rebuilt mid-flight.
  const animatedWidthRef = useRef(0);
  const directionRef = useRef(0);

  const startRef = useRef<() => void>(() => undefined);
  const stopRef = useRef<() => void>(() => undefined);

  const [animatedWidth, setAnimatedWidth] = useState(0);
  const [overflowWidth, setOverflowWidth] = useState(0);

  // Measure whenever the text itself changes. This is the only thing that
  // should trigger a re-measure, so overflowWidth is intentionally not a dep.
  useEffect(() => {
    const container = containerRef.current;
    const node = textRef.current;

    if (container && node) {
      const containerWidth = container.offsetWidth;
      const textWidth = node.offsetWidth;
      const newOverflowWidth = textWidth - containerWidth;

      setOverflowWidth((prevOverflowWidth) =>
        prevOverflowWidth === newOverflowWidth
          ? prevOverflowWidth
          : newOverflowWidth
      );
    }

    animatedWidthRef.current = 0;
    directionRef.current = 0;
    setAnimatedWidth(0);
  }, [text]);

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
