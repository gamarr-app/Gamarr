import { RefObject, useEffect, useMemo, useRef } from 'react';
import { CellComponentProps, Grid, GridImperativeAPI } from 'react-window';
import DiscoverGameItemConnector, {
  DiscoverGameProps,
} from 'DiscoverGame/DiscoverGameItemConnector';
import useMeasure from 'Helpers/Hooks/useMeasure';
import useVirtualScrollerSync from 'Helpers/Hooks/useVirtualScrollerSync';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import DiscoverGamePosterConnector from './DiscoverGamePosterConnector';
import styles from './DiscoverGamePosters.css';

// Poster container dimensions
const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);
const progressBarHeight = parseInt(dimensions.progressBarSmallHeight);
const detailedProgressBarHeight = parseInt(dimensions.progressBarMediumHeight);

const GRID_STYLE = {
  width: '100%',
  height: '100%',
  overflow: 'none',
};

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
  scrollerRef: RefObject<HTMLElement | null>;
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

  return Math.floor(
    width / (columns + (additionalColumnCount[posterSize] ?? 0))
  );
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

interface CellItemData {
  layout: {
    columnCount: number;
    padding: number;
    posterWidth: number;
    posterHeight: number;
  };
  items: DiscoverGameItem[];
  sortKey?: string;
  posterOptions: PosterOptions;
  showRelativeDates: boolean;
  shortDateFormat: string;
  timeFormat: string;
  selectedState: SelectedState;
  onSelectedChange: DiscoverGameProps['onSelectedChange'];
}

function Cell({
  columnIndex,
  rowIndex,
  style,
  layout,
  items,
  sortKey,
  posterOptions,
  showRelativeDates,
  shortDateFormat,
  timeFormat,
  selectedState,
  onSelectedChange,
}: CellComponentProps<CellItemData>) {
  const { columnCount, padding, posterWidth, posterHeight } = layout;
  const { showTitle, showIgdbRating, showMetacriticRating } = posterOptions;

  const index = rowIndex * columnCount + columnIndex;
  const game = items[index];

  if (!game) {
    return null;
  }

  return (
    <div
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
}

function DiscoverGamePosters({
  items,
  sortKey,
  posterOptions,
  jumpToCharacter,
  scrollerRef,
  showRelativeDates,
  shortDateFormat,
  timeFormat,
  isSmallScreen,
  selectedState,
  onSelectedChange,
}: DiscoverGamePostersProps) {
  const gridRef = useRef<GridImperativeAPI>(null);
  const [measureRef, bounds] = useMeasure();
  const previousJumpToCharacter = useRef(jumpToCharacter);

  const padding = useMemo(
    () => (isSmallScreen ? columnPaddingSmallScreen : columnPadding),
    [isSmallScreen]
  );

  // `bounds.width` comes from a ResizeObserver, so this recomputes whenever the
  // content body is resized rather than being measured once before layout.
  const width = bounds.width;

  const columnWidth = useMemo(
    () =>
      Math.max(
        calculateColumnWidth(width, posterOptions.size, isSmallScreen),
        1
      ),
    [width, posterOptions, isSmallScreen]
  );

  const columnCount = useMemo(
    () => Math.max(Math.floor(width / columnWidth), 1),
    [width, columnWidth]
  );

  const posterWidth = columnWidth - padding * 2;
  const posterHeight = useMemo(
    () => calculatePosterHeight(posterWidth),
    [posterWidth]
  );

  const rowHeight = useMemo(
    () =>
      calculateRowHeight(posterHeight, sortKey, isSmallScreen, posterOptions),
    [posterHeight, sortKey, isSmallScreen, posterOptions]
  );

  useVirtualScrollerSync({
    scrollerRef,
    virtualizedRef: gridRef,
    isSmallScreen,
  });

  useEffect(() => {
    if (
      jumpToCharacter != null &&
      jumpToCharacter !== previousJumpToCharacter.current
    ) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (index !== -1) {
        // Preserves react-virtualized's `scrollToAlignment="start"`: the target
        // row is aligned to the top of the viewport.
        const scrollTop = Math.floor(index / columnCount) * rowHeight;

        if (gridRef.current?.element) {
          gridRef.current.element.scrollTop = scrollTop;
        }

        scrollerRef.current?.scrollTo(0, scrollTop);
      }
    }

    previousJumpToCharacter.current = jumpToCharacter;
  }, [jumpToCharacter, items, columnCount, rowHeight, scrollerRef]);

  const cellProps = useMemo(
    () => ({
      layout: {
        columnCount,
        padding,
        posterWidth,
        posterHeight,
      },
      items,
      sortKey,
      posterOptions,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      selectedState,
      onSelectedChange,
    }),
    [
      columnCount,
      padding,
      posterWidth,
      posterHeight,
      items,
      sortKey,
      posterOptions,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      selectedState,
      onSelectedChange,
    ]
  );

  return (
    <div ref={measureRef}>
      <Grid<CellItemData>
        gridRef={gridRef}
        className={styles.grid}
        style={GRID_STYLE}
        columnCount={columnCount}
        columnWidth={columnWidth}
        rowCount={Math.ceil(items.length / columnCount)}
        rowHeight={rowHeight}
        overscanCount={2}
        cellProps={cellProps}
        cellComponent={Cell}
      />
    </div>
  );
}

export default DiscoverGamePosters;
