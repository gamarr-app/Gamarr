import { useCallback, useEffect, useRef, useState } from 'react';
import { Grid, GridCellRenderer, WindowScroller } from 'react-virtualized';
import Measure from 'Components/Measure';
import DiscoverGameItemConnector, {
  DiscoverGameProps,
} from 'DiscoverGame/DiscoverGameItemConnector';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import DiscoverGameOverviewConnector from './DiscoverGameOverviewConnector';
import styles from './DiscoverGameOverviews.css';

// Poster container dimensions
const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);

interface OverviewOptions {
  size: string;
}

interface DiscoverGameItem {
  igdbId: number;
  sortTitle: string;
  [key: string]: unknown;
}

interface SelectedState {
  [key: number]: boolean;
}

interface DiscoverGameOverviewsProps {
  items: DiscoverGameItem[];
  sortKey?: string;
  overviewOptions: OverviewOptions;
  jumpToCharacter?: string;
  scroller: Element;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  isSmallScreen: boolean;
  timeFormat: string;
  selectedState: SelectedState;
  onSelectedChange: DiscoverGameProps['onSelectedChange'];
}

function calculatePosterWidth(posterSize: string, isSmallScreen: boolean) {
  const maximumPosterWidth = isSmallScreen ? 152 : 162;

  if (posterSize === 'large') {
    return maximumPosterWidth;
  }

  if (posterSize === 'medium') {
    return Math.floor(maximumPosterWidth * 0.75);
  }

  return Math.floor(maximumPosterWidth * 0.5);
}

function calculateRowHeight(
  posterHeight: number,
  _sortKey: string | undefined,
  isSmallScreen: boolean,
  _overviewOptions: OverviewOptions
) {
  const heights = [
    posterHeight,
    isSmallScreen ? columnPaddingSmallScreen : columnPadding,
  ];

  return heights.reduce((acc, height) => acc + height, 0);
}

function calculatePosterHeight(posterWidth: number) {
  return Math.ceil((250 / 170) * posterWidth);
}

function DiscoverGameOverviews(props: DiscoverGameOverviewsProps) {
  const {
    items,
    sortKey,
    overviewOptions,
    jumpToCharacter,
    scroller,
    showRelativeDates,
    shortDateFormat,
    longDateFormat,
    timeFormat,
    isSmallScreen,
    selectedState,
    onSelectedChange,
  } = props;

  const gridRef = useRef<Grid | null>(null);

  const [width, setWidth] = useState(0);
  const [posterWidth, setPosterWidth] = useState(162);
  const [posterHeight, setPosterHeight] = useState(238);
  const [rowHeight, setRowHeight] = useState(
    calculateRowHeight(238, undefined, isSmallScreen, {} as OverviewOptions)
  );

  const prevItemsRef = useRef(items);
  const prevSortKeyRef = useRef(sortKey);
  const prevOverviewOptionsRef = useRef(overviewOptions);
  const prevJumpToCharacterRef = useRef(jumpToCharacter);
  const prevWidthRef = useRef(width);
  const prevRowHeightRef = useRef(rowHeight);

  const calculateGrid = useCallback(
    (newWidth: number = width) => {
      const newPosterWidth = calculatePosterWidth(
        overviewOptions.size,
        isSmallScreen
      );
      const newPosterHeight = calculatePosterHeight(newPosterWidth);
      const newRowHeight = calculateRowHeight(
        newPosterHeight,
        sortKey,
        isSmallScreen,
        overviewOptions
      );

      setWidth(newWidth);
      setPosterWidth(newPosterWidth);
      setPosterHeight(newPosterHeight);
      setRowHeight(newRowHeight);
    },
    [width, sortKey, overviewOptions, isSmallScreen]
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
      prevOverviewOptionsRef.current !== overviewOptions
    ) {
      calculateGrid(width);
    }

    if (
      gridRef.current &&
      (prevWidthRef.current !== width ||
        prevRowHeightRef.current !== rowHeight ||
        hasDifferentItemsOrOrder(prevItemsRef.current, items, 'igdbId') ||
        prevOverviewOptionsRef.current !== overviewOptions)
    ) {
      gridRef.current.recomputeGridSize();
    }

    if (
      jumpToCharacter != null &&
      jumpToCharacter !== prevJumpToCharacterRef.current
    ) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (gridRef.current && index != null) {
        gridScrollToCell({
          rowIndex: index,
          columnIndex: 0,
        });
      }
    }

    prevItemsRef.current = items;
    prevSortKeyRef.current = sortKey;
    prevOverviewOptionsRef.current = overviewOptions;
    prevJumpToCharacterRef.current = jumpToCharacter;
    prevWidthRef.current = width;
    prevRowHeightRef.current = rowHeight;
  }, [
    items,
    sortKey,
    overviewOptions,
    jumpToCharacter,
    width,
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
    ({ key, rowIndex, style }) => {
      const game = items[rowIndex];

      if (!game) {
        return null;
      }

      return (
        <div key={key} className={styles.container} style={style}>
          <DiscoverGameItemConnector
            key={game.igdbId}
            component={DiscoverGameOverviewConnector}
            sortKey={sortKey}
            posterWidth={posterWidth}
            posterHeight={posterHeight}
            rowHeight={rowHeight}
            overviewOptions={overviewOptions}
            showRelativeDates={showRelativeDates}
            shortDateFormat={shortDateFormat}
            longDateFormat={longDateFormat}
            timeFormat={timeFormat}
            isSmallScreen={isSmallScreen}
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
      posterWidth,
      posterHeight,
      rowHeight,
      overviewOptions,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat,
      isSmallScreen,
      selectedState,
      onSelectedChange,
    ]
  );

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
                columnCount={1}
                columnWidth={width}
                rowCount={items.length}
                rowHeight={rowHeight}
                width={width}
                scrollTop={scrollTop}
                overscanRowCount={2}
                cellRenderer={cellRenderer}
                selectedState={selectedState}
                scrollToAlignment="start"
                isScrollingOptout={true}
                onScroll={onChildScroll}
              />
            </div>
          );
        }}
      </WindowScroller>
    </Measure>
  );
}

export default DiscoverGameOverviews;
