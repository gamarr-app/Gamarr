import classNames from 'classnames';
import { ReactNode, RefObject, useEffect, useRef } from 'react';
import { List, ListImperativeAPI, ListProps } from 'react-window';
import Scroller from 'Components/Scroller/Scroller';
import useMeasure from 'Helpers/Hooks/useMeasure';
import usePrevious from 'Helpers/Hooks/usePrevious';
import useVirtualScrollerSync, {
  scrollToRowStart,
} from 'Helpers/Hooks/useVirtualScrollerSync';
import { scrollDirections } from 'Helpers/Props';
import styles from './VirtualTable.css';

const ROW_HEIGHT = 38;

const LIST_STYLE = {
  width: '100%',
  height: '100%',
  overflow: 'none',
};

interface VirtualTableProps<RowProps extends object> {
  isSmallScreen: boolean;
  className?: string;
  items: unknown[];
  scrollIndex?: number;
  scrollTop?: number;
  scrollerRef: RefObject<HTMLElement | null>;
  header: ReactNode;
  rowHeight?: number;
  rowComponent: ListProps<RowProps>['rowComponent'];
  rowProps: ListProps<RowProps>['rowProps'];
}

function VirtualTable<RowProps extends object>({
  isSmallScreen,
  className,
  items,
  scrollerRef,
  scrollIndex,
  scrollTop,
  header,
  rowHeight = ROW_HEIGHT,
  rowComponent,
  rowProps,
}: VirtualTableProps<RowProps>) {
  const [measureRef] = useMeasure();
  const listRef = useRef<ListImperativeAPI>(null);
  const scrollRestored = useRef(false);
  const previousScrollIndex = usePrevious(scrollIndex);

  useVirtualScrollerSync({
    scrollerRef,
    virtualizedRef: listRef,
    isSmallScreen,
  });

  useEffect(() => {
    if (scrollTop && !scrollRestored.current) {
      scrollRestored.current = true;

      if (listRef.current?.element) {
        listRef.current.element.scrollTop = scrollTop;
      }

      scrollerRef.current?.scrollTo(0, scrollTop);
    }
  }, [scrollTop, scrollerRef]);

  useEffect(() => {
    if (scrollIndex != null && scrollIndex !== previousScrollIndex) {
      scrollToRowStart({
        scrollerRef,
        virtualizedRef: listRef,
        rowIndex: scrollIndex,
        rowHeight,
      });
    }
  }, [scrollIndex, previousScrollIndex, rowHeight, scrollerRef]);

  return (
    <div ref={measureRef}>
      <Scroller
        className={classNames(styles.tableContainer, className)}
        scrollDirection={scrollDirections.HORIZONTAL}
      >
        {header}

        <List<RowProps>
          listRef={listRef}
          className={styles.tableBodyContainer}
          style={LIST_STYLE}
          rowCount={items.length}
          rowHeight={rowHeight}
          overscanCount={2}
          rowProps={rowProps}
          rowComponent={rowComponent}
        />
      </Scroller>
    </div>
  );
}

export default VirtualTable;
