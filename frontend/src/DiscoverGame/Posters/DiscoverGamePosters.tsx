import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Grid, GridCellRenderer, WindowScroller } from 'react-virtualized';
import Measure from 'Components/Measure';
import DiscoverGameItemConnector, {
  DiscoverGameProps,
} from 'DiscoverGame/DiscoverGameItemConnector';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import DiscoverGamePosterConnector from './DiscoverGamePosterConnector';
import styles from './DiscoverGamePosters.css';

// Poster container dimensions
const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);
const progressBarHeight = parseInt(dimensions.progressBarSmallHeight);
const detailedProgressBarHeight = parseInt(dimensions.progressBarMediumHeight);

const additionalColumnCount: Record<string, number> = {
  small: 3,
  medium: 2,
  large: 1,
};

interface PosterOptions {
  size: string;
  detailedProgressBar?: boolean;
  showTitle: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
}

interface DiscoverGameItem {
  igdbId: number;
  sortTitle: string;
  [key: string]: unknown;
}

interface SelectedState {
  [key: number]: boolean;
}

interface DiscoverGamePostersProps {
  items: DiscoverGameItem[];
  sortKey?: string;
  posterOptions: PosterOptions;
  jumpToCharacter?: string;
  scroller: Element;
  showRelativeDates: boolean;
  shortDateFormat: string;
  isSmallScreen: boolean;
  timeFormat: string;
  selectedState: SelectedState;
  onSelectedChange: DiscoverGameProps['onSelectedChange'];
}

function calculateColumnWidth(
  width: number,
  posterSize: string,
  isSmallScreen: boolean
) {
  const maximumColumnWidth = isSmallScreen ? 172 : 182;
  const columns = Math.floor(width / maximumColumnWidth);
  const remainder = width % maximumColumnWidth;

  if (remainder === 0 && posterSize === 'large') {
    return maximumColumnWidth;
  }

  return Math.floor(width / (columns + additionalColumnCount[posterSize]));
}

function calculateRowHeight(
  posterHeight: number,
  sortKey: string | undefined,
  isSmallScreen: boolean,
  posterOptions: PosterOptions
) {
  const {
    detailedProgressBar,
    showTitle,
    showIgdbRating,
    showMetacriticRating,
  } = posterOptions;

  const heights = [
    posterHeight,
    detailedProgressBar ? detailedProgressBarHeight : progressBarHeight,
    isSmallScreen ? columnPaddingSmallScreen : columnPadding,
  ];

  if (showTitle) {
    heights.push(19);
  }

  if (showIgdbRating) {
    heights.push(19);
  }

  if (showMetacriticRating) {
    heights.push(19);
  }

  switch (sortKey) {
    case 'studio':
    case 'inCinemas':
    case 'digitalRelease':
    case 'physicalRelease':
    case 'runtime':
    case 'certification':
      heights.push(19);
      break;
    case 'igdbRating':
      if (!showIgdbRating) {
        heights.push(19);
      }
      break;
    case 'metacriticRating':
      if (!showMetacriticRating) {
        heights.push(19);
      }
      break;
    default:
    // No need to add a height of 0
  }

  return heights.reduce((acc, height) => acc + height, 0);
}

function calculatePosterHeight(posterWidth: number) {
  return Math.ceil((250 / 170) * posterWidth);
}

function DiscoverGamePosters(props: DiscoverGamePostersProps) {
  const {
    items,
    sortKey,
    posterOptions,
    jumpToCharacter,
    scroller,
    showRelativeDates,
    shortDateFormat,
    timeFormat,
    isSmallScreen,
    selectedState,
    onSelectedChange,
  } = props;

  const gridRef = useRef<Grid | null>(null);

  const padding = useMemo(
    () => (isSmallScreen ? columnPaddingSmallScreen : columnPadding),
    [isSmallScreen]
  );

  const [width, setWidth] = useState(0);
  const [columnWidth, setColumnWidth] = useState(182);
  const [columnCount, setColumnCount] = useState(1);
  const [posterWidth, setPosterWidth] = useState(162);
  const [posterHeight, setPosterHeight] = useState(238);
  const [rowHeight, setRowHeight] = useState(
    calculateRowHeight(238, undefined, isSmallScreen, {} as PosterOptions)
  );

  const prevItemsRef = useRef(items);
  const prevSortKeyRef = useRef(sortKey);
  const prevPosterOptionsRef = useRef(posterOptions);
  const prevJumpToCharacterRef = useRef(jumpToCharacter);
  const prevWidthRef = useRef(width);
  const prevColumnWidthRef = useRef(columnWidth);
  const prevColumnCountRef = useRef(columnCount);
  const prevRowHeightRef = useRef(rowHeight);
  const prevSelectedStateRef = useRef(selectedState);

  const calculateGrid = useCallback(
    (newWidth: number = width) => {
      const newColumnWidth = calculateColumnWidth(
        newWidth,
        posterOptions.size,
        isSmallScreen
      );
      const newColumnCount = Math.max(Math.floor(newWidth / newColumnWidth), 1);
      const newPosterWidth = newColumnWidth - padding * 2;
      const newPosterHeight = calculatePosterHeight(newPosterWidth);
      const newRowHeight = calculateRowHeight(
        newPosterHeight,
        sortKey,
        isSmallScreen,
        posterOptions
      );

      setWidth(newWidth);
      setColumnWidth(newColumnWidth);
      setColumnCount(newColumnCount);
      setPosterWidth(newPosterWidth);
      setPosterHeight(newPosterHeight);
      setRowHeight(newRowHeight);
    },
    [width, sortKey, posterOptions, isSmallScreen, padding]
  );

  const gridScrollToPosition = useCallback(
    ({
      scrollTop = 0,
      scrollLeft = 0,
    }: {
      scrollTop?: number;
      scrollLeft?: number;
    }) => {
      scroller?.scrollTo({ top: scrollTop, left: scrollLeft });
    },
    [scroller]
  );

  const gridScrollToCell = useCallback(
    ({
      rowIndex = 0,
      columnIndex = 0,
    }: {
      rowIndex?: number;
      columnIndex?: number;
    }) => {
      const scrollOffset = gridRef.current?.getOffsetForCell({
        rowIndex,
        columnIndex,
      });

      if (scrollOffset) {
        gridScrollToPosition(scrollOffset);
      }
    },
    [gridScrollToPosition]
  );

  // Update effect
  useEffect(() => {
    if (
      prevSortKeyRef.current !== sortKey ||
      prevPosterOptionsRef.current !== posterOptions
    ) {
      calculateGrid(width);
    }

    if (
      gridRef.current &&
      (prevWidthRef.current !== width ||
        prevColumnWidthRef.current !== columnWidth ||
        prevColumnCountRef.current !== columnCount ||
        prevRowHeightRef.current !== rowHeight ||
        prevSelectedStateRef.current !== selectedState ||
        hasDifferentItemsOrOrder(prevItemsRef.current, items, 'igdbId'))
    ) {
      gridRef.current.recomputeGridSize();
    }

    if (
      jumpToCharacter != null &&
      jumpToCharacter !== prevJumpToCharacterRef.current
    ) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (gridRef.current && index != null) {
        const row = Math.floor(index / columnCount);

        gridScrollToCell({
          rowIndex: row,
          columnIndex: 0,
        });
      }
    }

    prevItemsRef.current = items;
    prevSortKeyRef.current = sortKey;
    prevPosterOptionsRef.current = posterOptions;
    prevJumpToCharacterRef.current = jumpToCharacter;
    prevWidthRef.current = width;
    prevColumnWidthRef.current = columnWidth;
    prevColumnCountRef.current = columnCount;
    prevRowHeightRef.current = rowHeight;
    prevSelectedStateRef.current = selectedState;
  }, [
    items,
    sortKey,
    posterOptions,
    jumpToCharacter,
    selectedState,
    width,
    columnWidth,
    columnCount,
    rowHeight,
    calculateGrid,
    gridScrollToCell,
  ]);

  const setGridRef = useCallback((ref: Grid | null) => {
    gridRef.current = ref;
  }, []);

  const onMeasure = useCallback(
    ({ width: measuredWidth = 0 }: { width?: number }) => {
      calculateGrid(measuredWidth);
    },
    [calculateGrid]
  );

  const cellRenderer: GridCellRenderer = useCallback(
    ({ key, rowIndex, columnIndex, style }) => {
      const { showTitle, showIgdbRating, showMetacriticRating } = posterOptions;

      const gameIdx = rowIndex * columnCount + columnIndex;
      const game = items[gameIdx];

      if (!game) {
        return null;
      }

      return (
        <div
          key={key}
          className={styles.container}
          style={{
            ...style,
            padding,
          }}
        >
          <DiscoverGameItemConnector
            key={game.igdbId}
            component={DiscoverGamePosterConnector}
            sortKey={sortKey}
            posterWidth={posterWidth}
            posterHeight={posterHeight}
            showTitle={showTitle}
            showIgdbRating={showIgdbRating}
            showMetacriticRating={showMetacriticRating}
            showRelativeDates={showRelativeDates}
            shortDateFormat={shortDateFormat}
            timeFormat={timeFormat}
            igdbId={game.igdbId}
            isSelected={selectedState[game.igdbId]}
            onSelectedChange={onSelectedChange}
          />
        </div>
      );
    },
    [
      items,
      sortKey,
      posterOptions,
      posterWidth,
      posterHeight,
      columnCount,
      padding,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      selectedState,
      onSelectedChange,
    ]
  );

  const rowCount = Math.ceil(items.length / columnCount);

  return (
    <Measure whitelist={['width']} onMeasure={onMeasure}>
      <WindowScroller scrollElement={isSmallScreen ? undefined : scroller}>
        {({ height, registerChild, onChildScroll, scrollTop }) => {
          if (!height) {
            return <div />;
          }

          return (
            <div ref={registerChild}>
              <Grid
                ref={setGridRef}
                className={styles.grid}
                autoHeight={true}
                height={height}
                columnCount={columnCount}
                columnWidth={columnWidth}
                rowCount={rowCount}
                rowHeight={rowHeight}
                width={width}
                scrollTop={scrollTop}
                overscanRowCount={2}
                cellRenderer={cellRenderer}
                selectedState={selectedState}
                scrollToAlignment="start"
                isScrollingOptOut={true}
                onScroll={onChildScroll}
              />
            </div>
          );
        }}
      </WindowScroller>
    </Measure>
  );
}

export default DiscoverGamePosters;
