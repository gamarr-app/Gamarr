import { RefObject, useEffect, useMemo, useRef } from 'react';
import { List, ListImperativeAPI, RowComponentProps } from 'react-window';
import CollectionItemConnector from 'Collection/CollectionItemConnector';
import useMeasure from 'Helpers/Hooks/useMeasure';
import useVirtualScrollerSync, {
  scrollToRowStart,
} from 'Helpers/Hooks/useVirtualScrollerSync';
import { CollectionItem } from 'Store/Selectors/createCollectionClientSideCollectionItemsSelector';
import { SelectStateInputProps } from 'typings/props';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import CollectionOverviewConnector from './CollectionOverviewConnector';
import styles from './CollectionOverviews.css';

// eslint-disable-next-line @typescript-eslint/no-require-imports
const dimensions = require('Styles/Variables/dimensions');

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

interface RowItemData {
  items: CollectionItem[];
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
  selectedState: Record<number, boolean>;
  onSelectedChange: (props: SelectStateInputProps) => void;
}

function Row({
  index,
  style,
  items,
  selectedState,
  ...otherData
}: RowComponentProps<RowItemData>) {
  const collection = items[index];

  if (!collection) {
    return null;
  }

  return (
    <div className={styles.container} style={style}>
      <CollectionItemConnector
        key={collection.id}
        component={CollectionOverviewConnector}
        collectionId={collection.id}
        isSelected={selectedState[collection.id]}
        {...otherData}
      />
    </div>
  );
}

interface CollectionOverviewsProps {
  items: CollectionItem[];
  sortKey?: string;
  sortDirection?: string;
  overviewOptions: OverviewOptions;
  jumpToCharacter?: string | null;
  scrollTop?: number;
  scrollerRef: RefObject<HTMLElement | null>;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  isSmallScreen: boolean;
  timeFormat: string;
  selectedState: Record<number, boolean>;
  onSelectedChange: (props: SelectStateInputProps) => void;
}

function CollectionOverviews({
  items,
  sortKey,
  overviewOptions,
  jumpToCharacter,
  scrollTop,
  scrollerRef,
  showRelativeDates,
  shortDateFormat,
  longDateFormat,
  timeFormat,
  isSmallScreen,
  selectedState,
  onSelectedChange,
}: CollectionOverviewsProps) {
  const listRef = useRef<ListImperativeAPI>(null);
  const [measureRef] = useMeasure();
  const scrollRestored = useRef(false);
  const previousJumpToCharacter = useRef(jumpToCharacter);

  const posterWidth = useMemo(
    () =>
      overviewOptions.showPosters
        ? calculatePosterWidth(overviewOptions.size, isSmallScreen)
        : 0,
    [overviewOptions, isSmallScreen]
  );

  const posterHeight = useMemo(
    () =>
      overviewOptions.showPosters ? calculatePosterHeight(posterWidth) : 0,
    [overviewOptions, posterWidth]
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
    if (scrollTop && !scrollRestored.current) {
      scrollRestored.current = true;

      if (listRef.current?.element) {
        listRef.current.element.scrollTop = scrollTop;
      }

      scrollerRef.current?.scrollTo(0, scrollTop);
    }
  }, [scrollTop, scrollerRef]);

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

export default CollectionOverviews;
