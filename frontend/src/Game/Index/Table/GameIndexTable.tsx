import { throttle } from 'lodash';
import { RefObject, useEffect, useMemo, useRef } from 'react';
import { useSelector } from 'react-redux';
import { List, ListImperativeAPI, RowComponentProps } from 'react-window';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Scroller from 'Components/Scroller/Scroller';
import Column from 'Components/Table/Column';
import useMeasure from 'Helpers/Hooks/useMeasure';
import { SortDirection } from 'Helpers/Props/sortDirections';
import { GameIndexItem } from 'Store/Selectors/createGameClientSideCollectionItemsSelector';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import GameIndexRow from './GameIndexRow';
import GameIndexTableHeader from './GameIndexTableHeader';
import styles from './GameIndexTable.css';

interface RowItemData {
  items: GameIndexItem[];
  sortKey: string;
  columns: Column[];
  isSelectMode: boolean;
}

interface GameIndexTableProps {
  items: GameIndexItem[];
  sortKey: string;
  sortDirection?: SortDirection;
  jumpToCharacter?: string;
  scrollTop?: number;
  scrollerRef: RefObject<HTMLElement | null>;
  isSelectMode: boolean;
  isSmallScreen: boolean;
}

const columnsSelector = createSelector(
  (state: AppState) => state.gameIndex.columns,
  (columns) => columns
);

function Row({
  index,
  style,
  items,
  sortKey,
  columns,
  isSelectMode,
}: RowComponentProps<RowItemData>) {
  if (index >= items.length) {
    return null;
  }

  const game = items[index];

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        ...style,
      }}
      className={styles.row}
    >
      <GameIndexRow
        gameId={game.id}
        sortKey={sortKey}
        columns={columns}
        isSelectMode={isSelectMode}
      />
    </div>
  );
}

function getWindowScrollTopPosition() {
  return document.documentElement.scrollTop || document.body.scrollTop || 0;
}

function GameIndexTable(props: GameIndexTableProps) {
  const {
    items,
    sortKey,
    sortDirection,
    jumpToCharacter,
    isSelectMode,
    isSmallScreen,
    scrollerRef,
  } = props;

  const columns = useSelector(columnsSelector);
  const listRef = useRef<ListImperativeAPI>(null);
  const [measureRef] = useMeasure();

  const rowHeight = useMemo(() => {
    return 38;
  }, []);

  useEffect(() => {
    const currentScrollerRef = scrollerRef.current as HTMLElement;
    const currentScrollListener = isSmallScreen ? window : currentScrollerRef;

    const handleScroll = throttle(() => {
      const { offsetTop = 0 } = currentScrollerRef;
      const scrollTop =
        (isSmallScreen
          ? getWindowScrollTopPosition()
          : currentScrollerRef.scrollTop) - offsetTop;

      if (listRef.current?.element) {
        listRef.current.element.scrollTop = scrollTop;
      }
    }, 10);

    currentScrollListener.addEventListener('scroll', handleScroll);

    return () => {
      handleScroll.cancel();

      if (currentScrollListener) {
        currentScrollListener.removeEventListener('scroll', handleScroll);
      }
    };
  }, [isSmallScreen, listRef, scrollerRef]);

  useEffect(() => {
    if (jumpToCharacter) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (index != null) {
        let scrollTop = index * rowHeight;

        // If the offset is zero go to the top, otherwise offset
        // by the approximate size of the header + padding (37 + 20).
        if (scrollTop > 0) {
          const offset = 57;

          scrollTop += offset;
        }

        if (listRef.current?.element) {
          listRef.current.element.scrollTop = scrollTop;
        }
        scrollerRef?.current?.scrollTo(0, scrollTop);
      }
    }
  }, [jumpToCharacter, rowHeight, items, scrollerRef, listRef]);

  return (
    <div ref={measureRef}>
      <Scroller className={styles.tableScroller} scrollDirection="horizontal">
        <GameIndexTableHeader
          columns={columns}
          sortKey={sortKey}
          sortDirection={sortDirection}
          isSelectMode={isSelectMode}
        />
        <List<RowItemData>
          listRef={listRef}
          style={{
            width: '100%',
            height: '100%',
            overflow: 'none',
          }}
          rowCount={items.length}
          rowHeight={rowHeight}
          rowProps={{
            items,
            sortKey,
            columns,
            isSelectMode,
          }}
          rowComponent={Row}
        />
      </Scroller>
    </div>
  );
}

export default GameIndexTable;
