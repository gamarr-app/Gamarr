import { useCallback, useEffect, useRef, useState } from 'react';
import { Grid, GridCellRenderer, WindowScroller } from 'react-virtualized';
import CollectionItemConnector from 'Collection/CollectionItemConnector';
import Measure from 'Components/Measure';
import { CollectionItem } from 'Store/Selectors/createCollectionClientSideCollectionItemsSelector';
import { SelectStateInputProps } from 'typings/props';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import CollectionOverviewConnector from './CollectionOverviewConnector';
import styles from './CollectionOverviews.css';

// eslint-disable-next-line @typescript-eslint/no-var-requires, @typescript-eslint/no-require-imports
const dimensions = require('Styles/Variables/dimensions');

// Poster container dimensions
const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);

interface OverviewOptions {
  showPosters: boolean;
  size: string;
  showDetails?: boolean;
  showOverview?: boolean;
  detailedProgressBar?: boolean;
}

function calculatePosterWidth(
  posterSize: string,
  isSmallScreen: boolean
): number {
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
  overviewOptions: OverviewOptions
): number {
  const heights = [
    overviewOptions.showPosters ? posterHeight : 75,
    isSmallScreen ? columnPaddingSmallScreen : columnPadding,
  ];

  return heights.reduce((acc, height) => acc + height + 80, 0);
}

function calculatePosterHeight(posterWidth: number): number {
  return Math.ceil((250 / 170) * posterWidth);
}

interface CollectionOverviewsProps {
  items: CollectionItem[];
  sortKey?: string;
  sortDirection?: string;
  overviewOptions: OverviewOptions;
  jumpToCharacter?: string | null;
  scrollTop?: number;
  scroller: HTMLDivElement | Element;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  isSmallScreen: boolean;
  timeFormat: string;
  selectedState: Record<number, boolean>;
  onSelectedChange: (props: SelectStateInputProps) => void;
}

function CollectionOverviews(props: CollectionOverviewsProps) {
  const {
    items,
    sortKey,
    overviewOptions,
    jumpToCharacter,
    scrollTop,
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
  const [scrollRestored, setScrollRestored] = useState(false);

  const prevItemsRef = useRef(items);
  const prevSortKeyRef = useRef(sortKey);
  const prevOverviewOptionsRef = useRef(overviewOptions);
  const prevJumpToCharacterRef = useRef(jumpToCharacter);
  const prevWidthRef = useRef(width);
  const prevRowHeightRef = useRef(rowHeight);

  const calculateGrid = useCallback(
    (newWidth: number = width) => {
      const newPosterWidth = overviewOptions.showPosters
        ? calculatePosterWidth(overviewOptions.size, isSmallScreen)
        : 0;
      const newPosterHeight = overviewOptions.showPosters
        ? calculatePosterHeight(newPosterWidth)
        : 0;
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
      scrollTop: top = 0,
      scrollLeft = 0,
    }: {
      scrollTop?: number;
      scrollLeft?: number;
    }) => {
      scroller?.scrollTo({ top, left: scrollLeft });
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
        hasDifferentItemsOrOrder(prevItemsRef.current, items) ||
        prevOverviewOptionsRef.current !== overviewOptions)
    ) {
      gridRef.current.recomputeGridSize();
    }

    if (gridRef.current && scrollTop !== 0 && !scrollRestored) {
      setScrollRestored(true);
      gridScrollToPosition({ scrollTop: scrollTop || 0 });
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
    scrollTop,
    width,
    rowHeight,
    scrollRestored,
    calculateGrid,
    gridScrollToPosition,
    gridScrollToCell,
  ]);

  const setGridRef = useCallback((ref: Grid | null) => {
    gridRef.current = ref;
  }, []);

  const onMeasure = useCallback(
    ({ width: measuredWidth = 0 }: { width?: number; height?: number }) => {
      calculateGrid(measuredWidth);
    },
    [calculateGrid]
  );

  const cellRenderer: GridCellRenderer = useCallback(
    ({ key, rowIndex, style }) => {
      const collection = items[rowIndex];

      if (!collection) {
        return null;
      }

      return (
        <div key={key} className={styles.container} style={style}>
          <CollectionItemConnector
            key={collection.id}
            component={CollectionOverviewConnector}
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
            collectionId={collection.id}
            isSelected={selectedState[collection.id]}
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
    <Measure onMeasure={onMeasure}>
      <div>
        <WindowScroller scrollElement={isSmallScreen ? undefined : scroller}>
          {({
            height,
            registerChild,
            onChildScroll,
            scrollTop: wsScrollTop,
          }) => {
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
                  scrollTop={wsScrollTop}
                  overscanRowCount={2}
                  cellRenderer={cellRenderer}
                  scrollToAlignment="start"
                  isScrollingOptOut={true}
                  onScroll={onChildScroll}
                />
              </div>
            );
          }}
        </WindowScroller>
      </div>
    </Measure>
  );
}

export default CollectionOverviews;
