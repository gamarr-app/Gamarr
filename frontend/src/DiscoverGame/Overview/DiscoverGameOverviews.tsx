import { Component } from 'react';
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

interface DiscoverGameOverviewsState {
  width: number;
  columnCount: number;
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
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

class DiscoverGameOverviews extends Component<
  DiscoverGameOverviewsProps,
  DiscoverGameOverviewsState
> {
  _grid: Grid | null = null;

  //
  // Lifecycle

  constructor(props: DiscoverGameOverviewsProps) {
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
    prevProps: DiscoverGameOverviewsProps,
    prevState: DiscoverGameOverviewsState
  ) {
    const { items, sortKey, overviewOptions, jumpToCharacter, isSmallScreen } =
      this.props;

    const { width, rowHeight } = this.state;

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
        hasDifferentItemsOrOrder(prevProps.items, items, 'igdbId') ||
        prevProps.overviewOptions !== overviewOptions)
    ) {
      // recomputeGridSize also forces Grid to discard its cache of rendered cells
      this._grid.recomputeGridSize();
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

  setGridRef = (ref: Grid) => {
    this._grid = ref;
  };

  calculateGrid = (
    width: number = this.state.width,
    isSmallScreen: boolean
  ) => {
    const { sortKey, overviewOptions } = this.props;

    const posterWidth = calculatePosterWidth(
      overviewOptions.size,
      isSmallScreen
    );
    const posterHeight = calculatePosterHeight(posterWidth);
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
  };

  _gridScrollToCell = ({
    rowIndex = 0,
    columnIndex = 0,
  }: {
    rowIndex?: number;
    columnIndex?: number;
  }) => {
    const scrollOffset = this._grid!.getOffsetForCell({
      rowIndex,
      columnIndex,
    });

    this._gridScrollToPosition(scrollOffset);
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

  onMeasure = ({ width = 0 }: { width?: number }) => {
    this.calculateGrid(width, this.props.isSmallScreen);
  };

  //
  // Render

  render() {
    const { isSmallScreen, scroller, items, selectedState } = this.props;

    const { width, rowHeight } = this.state;

    return (
      <Measure whitelist={['width']} onMeasure={this.onMeasure}>
        <WindowScroller scrollElement={isSmallScreen ? undefined : scroller}>
          {({ height, registerChild, onChildScroll, scrollTop }) => {
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
                  scrollTop={scrollTop}
                  overscanRowCount={2}
                  cellRenderer={this.cellRenderer}
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
}

export default DiscoverGameOverviews;
