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

  const [animatedWidth, setAnimatedWidth] = useState(0);
  const [overflowWidth, setOverflowWidth] = useState(0);
  const [direction, setDirection] = useState(0);

  const measureText = useCallback(() => {
    const container = containerRef.current;
    const node = textRef.current;

    if (container && node) {
      const containerWidth = container.offsetWidth;
      const textWidth = node.offsetWidth;
      const newOverflowWidth = textWidth - containerWidth;

      if (newOverflowWidth !== overflowWidth) {
        setOverflowWidth(newOverflowWidth);
      }
    }
  }, [overflowWidth]);

  const startAnimation = useCallback(() => {
    if (marqueeTimerRef.current) {
      clearTimeout(marqueeTimerRef.current);
    }

    const animate = () => {
      setAnimatedWidth((prevAnimatedWidth) => {
        setDirection((prevDirection) => {
          const newAnimatedWidth =
            prevDirection === 0
              ? prevAnimatedWidth + STEP
              : prevAnimatedWidth - STEP;
          let newDirection = prevDirection;

          const isRoundOver = newAnimatedWidth < 0;
          const endOfText = newAnimatedWidth > overflowWidth;

          if (endOfText) {
            newDirection = prevDirection === 1 ? 0 : 1;
          }

          if (isRoundOver) {
            if (loop) {
              newDirection = prevDirection === 0 ? 1 : 0;
            } else {
              return prevDirection;
            }
          }

          if (!isRoundOver || loop) {
            marqueeTimerRef.current = setTimeout(animate, TIMEOUT);
          }

          return newDirection;
        });

        const newAnimatedWidth =
          direction === 0 ? prevAnimatedWidth + STEP : prevAnimatedWidth - STEP;

        const isRoundOver = newAnimatedWidth < 0;

        if (isRoundOver && !loop) {
          return prevAnimatedWidth;
        }

        return newAnimatedWidth;
      });
    };

    const isLeading = animatedWidth === 0;
    const timeout = isLeading ? 0 : TIMEOUT;
    marqueeTimerRef.current = setTimeout(animate, timeout);
  }, [animatedWidth, direction, loop, overflowWidth]);

  const onHandleMouseEnter = useCallback(() => {
    if (hoverToStop) {
      if (marqueeTimerRef.current) {
        clearTimeout(marqueeTimerRef.current);
      }
    } else if (overflowWidth > 0) {
      startAnimation();
    }
  }, [hoverToStop, overflowWidth, startAnimation]);

  const onHandleMouseLeave = useCallback(() => {
    if (hoverToStop && overflowWidth > 0) {
      startAnimation();
    } else {
      if (marqueeTimerRef.current) {
        clearTimeout(marqueeTimerRef.current);
      }
      setAnimatedWidth(0);
    }
  }, [hoverToStop, overflowWidth, startAnimation]);

  // Reset animation when text changes
  useEffect(() => {
    if (marqueeTimerRef.current) {
      clearTimeout(marqueeTimerRef.current);
    }
    setAnimatedWidth(0);
    setDirection(0);
  }, [text.length]);

  // Measure and start animation
  useEffect(() => {
    measureText();

    if (hoverToStop) {
      startAnimation();
    }
  }, [measureText, hoverToStop, startAnimation]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (marqueeTimerRef.current) {
        clearTimeout(marqueeTimerRef.current);
      }
    };
  }, []);

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
