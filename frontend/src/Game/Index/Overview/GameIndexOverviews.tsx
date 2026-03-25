import { throttle } from 'lodash';
import { RefObject, useEffect, useMemo, useRef } from 'react';
import { useSelector } from 'react-redux';
import { List, ListImperativeAPI, RowComponentProps } from 'react-window';
import useMeasure from 'Helpers/Hooks/useMeasure';
import { GameIndexItem } from 'Store/Selectors/createGameClientSideCollectionItemsSelector';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import GameIndexOverview from './GameIndexOverview';
import selectOverviewOptions from './selectOverviewOptions';

// Poster container dimensions
const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);
const progressBarHeight = parseInt(dimensions.progressBarSmallHeight);
const detailedProgressBarHeight = parseInt(dimensions.progressBarMediumHeight);

interface RowItemData {
  items: GameIndexItem[];
  sortKey: string;
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
  isSelectMode: boolean;
  isSmallScreen: boolean;
}

interface GameIndexOverviewsProps {
  items: GameIndexItem[];
  sortKey: string;
  sortDirection?: string;
  jumpToCharacter?: string;
  scrollTop?: number;
  scrollerRef: RefObject<HTMLElement | null>;
  isSelectMode: boolean;
  isSmallScreen: boolean;
}

function Row({
  index,
  style,
  items,
  ...otherData
}: RowComponentProps<RowItemData>) {
  if (index >= items.length) {
    return null;
  }

  const game = items[index];

  return (
    <div style={style}>
      <GameIndexOverview gameId={game.id} {...otherData} />
    </div>
  );
}

function getWindowScrollTopPosition() {
  return document.documentElement.scrollTop || document.body.scrollTop || 0;
}

function GameIndexOverviews(props: GameIndexOverviewsProps) {
  const {
    items,
    sortKey,
    jumpToCharacter,
    scrollerRef,
    isSelectMode,
    isSmallScreen,
  } = props;

  const { size: posterSize, detailedProgressBar } = useSelector(
    selectOverviewOptions
  );
  const listRef = useRef<ListImperativeAPI>(null);
  const [measureRef] = useMeasure();

  const posterWidth = useMemo(() => {
    const maximumPosterWidth = isSmallScreen ? 152 : 162;

    if (posterSize === 'large') {
      return maximumPosterWidth;
    }

    if (posterSize === 'medium') {
      return Math.floor(maximumPosterWidth * 0.75);
    }

    return Math.floor(maximumPosterWidth * 0.5);
  }, [posterSize, isSmallScreen]);

  const posterHeight = useMemo(() => {
    return Math.ceil((250 / 170) * posterWidth);
  }, [posterWidth]);

  const rowHeight = useMemo(() => {
    const heights = [
      posterHeight,
      detailedProgressBar ? detailedProgressBarHeight : progressBarHeight,
      isSmallScreen ? columnPaddingSmallScreen : columnPadding,
    ];

    return heights.reduce((acc, height) => acc + height, 0);
  }, [detailedProgressBar, posterHeight, isSmallScreen]);

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
        scrollerRef.current?.scrollTo(0, scrollTop);
      }
    }
  }, [jumpToCharacter, rowHeight, items, scrollerRef, listRef]);

  return (
    <div ref={measureRef}>
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
          posterWidth,
          posterHeight,
          rowHeight,
          isSelectMode,
          isSmallScreen,
        }}
        rowComponent={Row}
      />
    </div>
  );
}

export default GameIndexOverviews;
