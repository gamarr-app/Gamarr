import { Placement } from '@popperjs/core';
import classNames from 'classnames';
import React, {
  ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { usePopper } from 'react-popper';
import Portal from 'Components/Portal';
import { kinds, tooltipPositions } from 'Helpers/Props';
import { Kind } from 'Helpers/Props/kinds';
import dimensions from 'Styles/Variables/dimensions';
import { isMobile as isMobileUtil } from 'Utilities/browser';
import styles from './Tooltip.css';

export interface TooltipProps {
  className?: string;
  bodyClassName?: string;
  anchor: ReactNode;
  tooltip: string | ReactNode;
  kind?: Extract<Kind, keyof typeof styles>;
  position?: (typeof tooltipPositions.all)[number];
  canFlip?: boolean;
}
function Tooltip(props: TooltipProps) {
  const {
    className,
    bodyClassName = styles.body,
    anchor,
    tooltip,
    kind = kinds.DEFAULT,
    position = tooltipPositions.TOP,
    canFlip = false,
  } = props;

  const closeTimeout = useRef<ReturnType<typeof setTimeout>>(undefined);
  const [referenceElement, setReferenceElement] = useState<HTMLElement | null>(
    null
  );
  const [popperElement, setPopperElement] = useState<HTMLElement | null>(null);
  const [arrowElement, setArrowElement] = useState<HTMLElement | null>(null);
  const [isOpen, setIsOpen] = useState(false);

  const handleClick = useCallback(() => {
    if (!isMobileUtil()) {
      return;
    }

    setIsOpen((isOpen) => {
      return !isOpen;
    });
  }, [setIsOpen]);

  const handleMouseEnterAnchor = useCallback(() => {
    // Mobile will fire mouse enter and click events rapidly,
    // this causes the tooltip not to open on the first press.
    // Ignore the mouse enter event on mobile.

    if (isMobileUtil()) {
      return;
    }

    if (closeTimeout.current) {
      clearTimeout(closeTimeout.current);
    }

    setIsOpen(true);
  }, [setIsOpen]);

  const handleMouseEnterTooltip = useCallback(() => {
    if (closeTimeout.current) {
      clearTimeout(closeTimeout.current);
    }

    setIsOpen(true);
  }, [setIsOpen]);

  const handleMouseLeave = useCallback(() => {
    // Still listen for mouse leave on mobile to allow clicks outside to close the tooltip.

    clearTimeout(closeTimeout.current);
    closeTimeout.current = setTimeout(() => {
      setIsOpen(false);
    }, 100);
  }, [setIsOpen]);

  const maxWidth = useMemo(() => {
    const windowWidth = window.innerWidth;

    if (windowWidth >= parseInt(dimensions.breakpointLarge)) {
      return 800;
    } else if (windowWidth >= parseInt(dimensions.breakpointMedium)) {
      return 650;
    } else if (windowWidth >= parseInt(dimensions.breakpointSmall)) {
      return 500;
    }

    return 450;
  }, []);

  const popperModifiers = useMemo(
    () => [
      {
        name: 'arrow',
        options: {
          element: arrowElement,
        },
      },
      {
        name: 'eventListeners',
        enabled: false,
      },
      {
        name: 'computeMaxSize',
        enabled: true,
        phase: 'beforeWrite' as const,
        requires: ['computeStyles'],
        fn: ({
          state,
        }: {
          state: {
            rects: {
              reference: {
                x: number;
                y: number;
                width: number;
                height: number;
              };
            };
            placement: string;
            styles: { popper: Record<string, string> };
          };
        }) => {
          const ref = state.rects.reference;
          const top = ref.y;
          const right = ref.x + ref.width;
          const bottom = ref.y + ref.height;
          const left = ref.x;

          const windowWidth = window.innerWidth;
          const windowHeight = window.innerHeight;

          if (/^top/.test(state.placement)) {
            state.styles.popper.maxHeight = `${top - 20}px`;
          } else if (/^bottom/.test(state.placement)) {
            state.styles.popper.maxHeight = `${windowHeight - bottom - 20}px`;
          } else if (/^right/.test(state.placement)) {
            state.styles.popper.maxWidth = `${Math.min(maxWidth, windowWidth - right - 20)}px`;
            state.styles.popper.maxHeight = `${top - 20}px`;
          } else {
            state.styles.popper.maxWidth = `${Math.min(maxWidth, left - 20)}px`;
            state.styles.popper.maxHeight = `${top - 20}px`;
          }
        },
      },
      {
        name: 'preventOverflow',
        options: {
          escapeWithReference: false,
        },
      },
      {
        name: 'flip',
        enabled: canFlip,
      },
    ],
    [arrowElement, maxWidth, canFlip]
  );

  const {
    styles: popperStyles,
    attributes,
    update,
  } = usePopper(referenceElement, popperElement, {
    placement: position as Placement,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    modifiers: popperModifiers as any,
  });

  useEffect(() => {
    if (update && isOpen) {
      update();
    }
  });

  useEffect(() => {
    return () => {
      if (closeTimeout.current) {
        clearTimeout(closeTimeout.current);
      }
    };
  }, []);

  const popperPlacement = attributes.popper?.['data-popper-placement']
    ? (attributes.popper['data-popper-placement'] as string).split('-')[0]
    : position;
  const vertical = popperPlacement === 'top' || popperPlacement === 'bottom';

  return (
    <>
      <span
        ref={setReferenceElement as unknown as React.Ref<HTMLSpanElement>}
        className={className}
        onClick={handleClick}
        onMouseEnter={handleMouseEnterAnchor}
        onMouseLeave={handleMouseLeave}
      >
        {anchor}
      </span>

      <Portal>
        <div
          ref={setPopperElement}
          className={classNames(
            styles.tooltipContainer,
            vertical ? styles.verticalContainer : styles.horizontalContainer
          )}
          style={popperStyles.popper}
          {...attributes.popper}
          onMouseEnter={handleMouseEnterTooltip}
          onMouseLeave={handleMouseLeave}
        >
          <div
            ref={setArrowElement as unknown as React.Ref<HTMLDivElement>}
            className={
              isOpen
                ? classNames(
                    styles.arrow,
                    styles[kind],
                    styles[popperPlacement as keyof typeof styles]
                  )
                : styles.arrowDisabled
            }
            style={popperStyles.arrow}
          />
          {isOpen ? (
            <div className={classNames(styles.tooltip, styles[kind])}>
              <div className={bodyClassName}>{tooltip}</div>
            </div>
          ) : null}
        </div>
      </Portal>
    </>
  );
}

export default Tooltip;
