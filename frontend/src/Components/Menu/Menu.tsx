import { Placement } from '@popperjs/core';
import React, {
  CSSProperties,
  ReactElement,
  Ref,
  useCallback,
  useEffect,
  useId,
  useMemo,
  useState,
} from 'react';
import { usePopper } from 'react-popper';
import Portal from 'Components/Portal';
import styles from './Menu.css';

interface MenuButtonChildProps {
  onPress?: () => void;
}

interface MenuContentChildProps {
  forwardedRef?: Ref<HTMLDivElement>;
  style?: CSSProperties;
  isOpen?: boolean;
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

      if (event.targetTouches.length !== 1) {
        return;
      }

      const target = event.targetTouches[0].target;

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

  const handleMenuButtonPress = useCallback(() => {
    setIsMenuOpen((isOpen) => !isOpen);
  }, []);

  const childrenArray = React.Children.toArray(children);
  const button = React.cloneElement(
    childrenArray[0] as ReactElement<MenuButtonChildProps>,
    {
      onPress: handleMenuButtonPress,
    }
  );

  useEffect(() => {
    if (enforceMaxHeight) {
      updateMaxHeight();
    }
  }, [enforceMaxHeight, updateMaxHeight]);

  useEffect(() => {
    if (update && isMenuOpen) {
      update();
    }
  }, [isMenuOpen, update]);

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

    return () => {
      window.removeEventListener('resize', handleWindowResize);
      window.removeEventListener('scroll', handleWindowScroll, {
        capture: true,
      });
      window.removeEventListener('click', handleWindowClick);
      window.removeEventListener('touchstart', handleTouchStart);
    };
  }, [
    isMenuOpen,
    handleWindowResize,
    handleWindowScroll,
    handleWindowClick,
    handleTouchStart,
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
          }
        )}
      </Portal>
    </>
  );
}

export default Menu;
