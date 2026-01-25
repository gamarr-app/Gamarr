import { ReactNode, Ref, useCallback } from 'react';
import Scroller, { OnScroll } from 'Components/Scroller/Scroller';
import { isLocked } from 'Utilities/scrollLock';
import styles from './PageContentBody.css';

interface PageContentBodyProps {
  ref?: Ref<HTMLDivElement>;
  className?: string;
  innerClassName?: string;
  children: ReactNode;
  initialScrollTop?: number;
  onScroll?: (payload: OnScroll) => void;
}

function PageContentBody(props: PageContentBodyProps) {
  const {
    ref,
    className = styles.contentBody,
    innerClassName = styles.innerContentBody,
    children,
    onScroll,
    ...otherProps
  } = props;

  const onScrollWrapper = useCallback(
    (payload: OnScroll) => {
      if (onScroll && !isLocked()) {
        onScroll(payload);
      }
    },
    [onScroll]
  );

  return (
    <Scroller
      ref={ref}
      {...otherProps}
      className={className}
      scrollDirection="vertical"
      onScroll={onScrollWrapper}
    >
      <div className={innerClassName}>{children}</div>
    </Scroller>
  );
}

export default PageContentBody;
