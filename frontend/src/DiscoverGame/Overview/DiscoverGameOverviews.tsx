import { RefObject, useEffect, useMemo, useRef } from 'react';
import { List, ListImperativeAPI, RowComponentProps } from 'react-window';
import DiscoverGameItemConnector, {
  DiscoverGameProps,
} from 'DiscoverGame/DiscoverGameItemConnector';
import useMeasure from 'Helpers/Hooks/useMeasure';
import useVirtualScrollerSync, {
  scrollToRowStart,
} from 'Helpers/Hooks/useVirtualScrollerSync';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import DiscoverGameOverviewConnector from './DiscoverGameOverviewConnector';
import styles from './DiscoverGameOverviews.css';

// Poster container dimensions
const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);

const LIST_STYLE = {
  width: '100%',
  height: '100%',
  overflow: 'none',
};

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
  scrollerRef: RefObject<HTMLElement | null>;
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

interface RowItemData {
  items: DiscoverGameItem[];
  sortKey?: string;
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
  overviewOptions: OverviewOptions;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  timeFormat: string;
  isSmallScreen: boolean;
  selectedState: SelectedState;
  onSelectedChange: DiscoverGameProps['onSelectedChange'];
}

function Row({
  index,
  style,
  items,
  selectedState,
  ...otherData
}: RowComponentProps<RowItemData>) {
  const game = items[index];

  if (!game) {
    return null;
  }

  return (
    <div className={styles.container} style={style}>
      <DiscoverGameItemConnector
        key={game.igdbId}
        component={DiscoverGameOverviewConnector}
        igdbId={game.igdbId}
        isSelected={selectedState[game.igdbId]}
        {...otherData}
      />
    </div>
  );
}

function DiscoverGameOverviews({
  items,
  sortKey,
  overviewOptions,
  jumpToCharacter,
  scrollerRef,
  showRelativeDates,
  shortDateFormat,
  longDateFormat,
  timeFormat,
  isSmallScreen,
  selectedState,
  onSelectedChange,
}: DiscoverGameOverviewsProps) {
  const listRef = useRef<ListImperativeAPI>(null);
  const [measureRef] = useMeasure();
  const previousJumpToCharacter = useRef(jumpToCharacter);

  const posterWidth = useMemo(
    () => calculatePosterWidth(overviewOptions.size, isSmallScreen),
    [overviewOptions, isSmallScreen]
  );

  const posterHeight = useMemo(
    () => calculatePosterHeight(posterWidth),
    [posterWidth]
  );

  const rowHeight = useMemo(
    () =>
      calculateRowHeight(posterHeight, sortKey, isSmallScreen, overviewOptions),
    [posterHeight, sortKey, isSmallScreen, overviewOptions]
  );

  useVirtualScrollerSync({
    scrollerRef,
    virtualizedRef: listRef,
    isSmallScreen,
  });

  useEffect(() => {
    if (
      jumpToCharacter != null &&
      jumpToCharacter !== previousJumpToCharacter.current
    ) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (index !== -1) {
        scrollToRowStart({
          scrollerRef,
          virtualizedRef: listRef,
          rowIndex: index,
          rowHeight,
        });
      }
    }

    previousJumpToCharacter.current = jumpToCharacter;
  }, [jumpToCharacter, items, rowHeight, scrollerRef]);

  const rowProps = useMemo(
    () => ({
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
    }),
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
    <div ref={measureRef}>
      <List<RowItemData>
        listRef={listRef}
        className={styles.grid}
        style={LIST_STYLE}
        rowCount={items.length}
        rowHeight={rowHeight}
        overscanCount={2}
        rowProps={rowProps}
        rowComponent={Row}
      />
    </div>
  );
}

export default DiscoverGameOverviews;
