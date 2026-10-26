import { Placement } from '@popperjs/core';
import React, {
  CSSProperties,
  ReactElement,
  Ref,
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
} from 'react';
import { usePopper } from 'react-popper';
import Portal from 'Components/Portal';
import styles from './Menu.css';

interface MenuButtonChildProps {
  onPress?: () => void;
  'aria-haspopup'?: 'menu';
  'aria-expanded'?: boolean;
  'aria-controls'?: string;
}

interface MenuContentChildProps {
  forwardedRef?: Ref<HTMLDivElement>;
  style?: CSSProperties;
  isOpen?: boolean;
  id?: string;
  role?: string;
  onKeyDown?: (event: React.KeyboardEvent<HTMLDivElement>) => void;
}

const placementMap: Record<string, Placement> = {
  right: 'bottom-end',
  left: 'bottom-start',
};

interface MenuProps {
  className?: string;
  children: React.ReactNode;
  alignMenu?: 'left' | 'right';
  enforceMaxHeight?: boolean;
}

function Menu({
  className = styles.menu,
  children,
  alignMenu = 'left',
  enforceMaxHeight = true,
}: MenuProps) {
  const [referenceElement, setReferenceElement] = useState<HTMLElement | null>(
    null
  );
  const [popperElement, setPopperElement] = useState<HTMLElement | null>(null);
  const menuButtonId = useId();
  const menuContentId = useId();
  const [maxHeight, setMaxHeight] = useState(0);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const shouldRestoreFocus = useRef(false);

  const popperModifiers = useMemo(
    () => [
      {
        name: 'preventOverflow',
        options: {
          padding: 0,
        },
      },
      {
        name: 'flip',
        options: {
          padding: 0,
        },
      },
    ],
    []
  );

  const {
    styles: popperStyles,
    attributes,
    update,
  } = usePopper(referenceElement, popperElement, {
    placement: placementMap[alignMenu],
    modifiers: popperModifiers,
  });

  const updateMaxHeight = useCallback(() => {
    const menuButton = document.getElementById(menuButtonId);

    if (!menuButton) {
      setMaxHeight(0);

      return;
    }

    const { bottom } = menuButton.getBoundingClientRect();
    const height = window.innerHeight - bottom;

    setMaxHeight(height);
  }, [menuButtonId]);

  const handleWindowClick = useCallback(
    (event: MouseEvent) => {
      const menuButton = document.getElementById(menuButtonId);

      if (!menuButton) {
        return;
      }

      if (!menuButton.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    },
    [menuButtonId]
  );

  const handleTouchStart = useCallback(
    (event: TouchEvent) => {
      const menuButton = document.getElementById(menuButtonId);
      const menuContent = document.getElementById(menuContentId);

      if (!menuButton || !menuContent) {
        return;
      }

      const touch = event.targetTouches[0];

      if (event.targetTouches.length !== 1 || !touch) {
        return;
      }

      const target = touch.target;

      if (
        !menuButton.contains(target as Node) &&
        !menuContent.contains(target as Node)
      ) {
        setIsMenuOpen(false);
      }
    },
    [menuButtonId, menuContentId]
  );

  const handleWindowResize = useCallback(() => {
    updateMaxHeight();
  }, [updateMaxHeight]);

  const handleWindowScroll = useCallback(() => {
    if (isMenuOpen) {
      updateMaxHeight();
    }
  }, [isMenuOpen, updateMaxHeight]);

  const focusMenuButton = useCallback(() => {
    if (document.activeElement !== document.body) {
      return;
    }

    document
      .getElementById(menuButtonId)
      ?.querySelector<HTMLElement>('button, a[href]')
      ?.focus();
  }, [menuButtonId]);

  useEffect(() => {
    if (!isMenuOpen && shouldRestoreFocus.current) {
      shouldRestoreFocus.current = false;
      const focusTimeout = window.setTimeout(focusMenuButton, 100);

      return () => window.clearTimeout(focusTimeout);
    }

    return undefined;
  }, [focusMenuButton, isMenuOpen]);

  const handleWindowKeyDown = useCallback((event: KeyboardEvent) => {
    if (event.key !== 'Escape') {
      return;
    }

    shouldRestoreFocus.current = true;
    setIsMenuOpen(false);
  }, []);

  const handleMenuKeyDown = useCallback(
    (event: React.KeyboardEvent<HTMLDivElement>) => {
      if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
        return;
      }

      const items = Array.from(
        event.currentTarget.querySelectorAll<HTMLElement>(
          '[role^="menuitem"]:not(:disabled):not([aria-disabled="true"])'
        )
      );

      if (!items.length) {
        return;
      }

      event.preventDefault();

      const currentIndex = items.indexOf(document.activeElement as HTMLElement);
      let nextIndex = currentIndex;

      if (event.key === 'Home') {
        nextIndex = 0;
      } else if (event.key === 'End') {
        nextIndex = items.length - 1;
      } else if (event.key === 'ArrowDown') {
        nextIndex = currentIndex < items.length - 1 ? currentIndex + 1 : 0;
      } else {
        nextIndex = currentIndex > 0 ? currentIndex - 1 : items.length - 1;
      }

      items[nextIndex]?.focus();
    },
    []
  );

  const handleMenuButtonPress = useCallback(() => {
    setIsMenuOpen((isOpen) => !isOpen);
  }, []);

  const childrenArray = React.Children.toArray(children);
  const button = React.cloneElement(
    childrenArray[0] as ReactElement<MenuButtonChildProps>,
    {
      onPress: handleMenuButtonPress,
      'aria-haspopup': 'menu',
      'aria-expanded': isMenuOpen,
      'aria-controls': isMenuOpen ? menuContentId : undefined,
    }
  );

  useEffect(() => {
    if (enforceMaxHeight) {
      updateMaxHeight();
    }
  }, [enforceMaxHeight, updateMaxHeight]);

  const updateRef = useRef(update);
  updateRef.current = update;

  useEffect(() => {
    if (updateRef.current && isMenuOpen) {
      updateRef.current();
    }
  }, [isMenuOpen]);

  useEffect(() => {
    // Listen to resize events on the window and scroll events
    // on all elements to ensure the menu is the best size possible.
    // Listen for click events on the window to support closing the
    // menu on clicks outside.

    if (!isMenuOpen) {
      return;
    }

    window.addEventListener('resize', handleWindowResize);
    window.addEventListener('scroll', handleWindowScroll, { capture: true });
    window.addEventListener('click', handleWindowClick);
    window.addEventListener('touchstart', handleTouchStart);
    window.addEventListener('keydown', handleWindowKeyDown);

    return () => {
      window.removeEventListener('resize', handleWindowResize);
      window.removeEventListener('scroll', handleWindowScroll, {
        capture: true,
      });
      window.removeEventListener('click', handleWindowClick);
      window.removeEventListener('touchstart', handleTouchStart);
      window.removeEventListener('keydown', handleWindowKeyDown);
    };
  }, [
    isMenuOpen,
    handleWindowResize,
    handleWindowScroll,
    handleWindowClick,
    handleTouchStart,
    handleWindowKeyDown,
  ]);

  return (
    <>
      <div ref={setReferenceElement} id={menuButtonId} className={className}>
        {button}
      </div>
      <Portal>
        {React.cloneElement(
          childrenArray[1] as ReactElement<MenuContentChildProps>,
          {
            forwardedRef: (node: HTMLDivElement | null) => {
              setPopperElement(node);
            },
            style: {
              ...popperStyles.popper,
              maxHeight,
            },
            ...attributes.popper,
            isOpen: isMenuOpen,
            id: menuContentId,
            role: 'menu',
            onKeyDown: handleMenuKeyDown,
          }
        )}
      </Portal>
    </>
  );
}

export default Menu;
