import { Component } from 'react';
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

interface CollectionOverviewsState {
  width: number;
  columnCount: number;
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
  scrollRestored?: boolean;
}

class CollectionOverviews extends Component<
  CollectionOverviewsProps,
  CollectionOverviewsState
> {
  // eslint-disable-next-line react/sort-comp
  private _grid: Grid | null = null;

  //
  // Lifecycle

  constructor(props: CollectionOverviewsProps) {
    super(props);

    this.state = {
      width: 0,
      columnCount: 1,
      posterWidth: 162,
      posterHeight: 238,
      rowHeight: calculateRowHeight(
        238,
        undefined,
        props.isSmallScreen,
        {} as OverviewOptions
      ),
    };
  }

  componentDidUpdate(
    prevProps: CollectionOverviewsProps,
    prevState: CollectionOverviewsState
  ) {
    const {
      items,
      sortKey,
      overviewOptions,
      jumpToCharacter,
      scrollTop,
      isSmallScreen,
    } = this.props;

    const { width, rowHeight, scrollRestored } = this.state;

    if (
      prevProps.sortKey !== sortKey ||
      prevProps.overviewOptions !== overviewOptions
    ) {
      this.calculateGrid(this.state.width, isSmallScreen);
    }

    if (
      this._grid &&
      (prevState.width !== width ||
        prevState.rowHeight !== rowHeight ||
        hasDifferentItemsOrOrder(prevProps.items, items) ||
        prevProps.overviewOptions !== overviewOptions)
    ) {
      // recomputeGridSize also forces Grid to discard its cache of rendered cells
      this._grid.recomputeGridSize();
    }

    if (this._grid && scrollTop !== 0 && !scrollRestored) {
      this.setState({ scrollRestored: true });
      this._gridScrollToPosition({ scrollTop: scrollTop || 0 });
    }

    if (
      jumpToCharacter != null &&
      jumpToCharacter !== prevProps.jumpToCharacter
    ) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (this._grid && index != null) {
        this._gridScrollToCell({
          rowIndex: index,
          columnIndex: 0,
        });
      }
    }
  }

  //
  // Control

  setGridRef = (ref: Grid | null) => {
    this._grid = ref;
  };

  calculateGrid = (
    width: number = this.state.width,
    isSmallScreen: boolean
  ) => {
    const { sortKey, overviewOptions } = this.props;

    const posterWidth = overviewOptions.showPosters
      ? calculatePosterWidth(overviewOptions.size, isSmallScreen)
      : 0;
    const posterHeight = overviewOptions.showPosters
      ? calculatePosterHeight(posterWidth)
      : 0;
    const rowHeight = calculateRowHeight(
      posterHeight,
      sortKey,
      isSmallScreen,
      overviewOptions
    );

    this.setState({
      width,
      posterWidth,
      posterHeight,
      rowHeight,
    });
  };

  cellRenderer: GridCellRenderer = ({ key, rowIndex, style }) => {
    const {
      items,
      sortKey,
      overviewOptions,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat,
      isSmallScreen,
      selectedState,
      onSelectedChange,
    } = this.props;

    const { posterWidth, posterHeight, rowHeight } = this.state;

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
  };

  _gridScrollToCell = ({
    rowIndex = 0,
    columnIndex = 0,
  }: {
    rowIndex?: number;
    columnIndex?: number;
  }) => {
    const scrollOffset = this._grid?.getOffsetForCell({
      rowIndex,
      columnIndex,
    });

    if (scrollOffset) {
      this._gridScrollToPosition(scrollOffset);
    }
  };

  _gridScrollToPosition = ({
    scrollTop = 0,
    scrollLeft = 0,
  }: {
    scrollTop?: number;
    scrollLeft?: number;
  }) => {
    this.props.scroller?.scrollTo({ top: scrollTop, left: scrollLeft });
  };

  //
  // Listeners

  onMeasure = ({ width = 0 }: { width?: number; height?: number }) => {
    this.calculateGrid(width, this.props.isSmallScreen);
  };

  //
  // Render

  render() {
    const { isSmallScreen, scroller, items } = this.props;

    const { width, rowHeight } = this.state;

    return (
      <Measure onMeasure={this.onMeasure}>
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
                    ref={this.setGridRef}
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
                    cellRenderer={this.cellRenderer}
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
}

export default CollectionOverviews;
