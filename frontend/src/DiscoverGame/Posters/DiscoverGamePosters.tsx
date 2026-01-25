import { Component } from 'react';
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

interface DiscoverGamePostersState {
  width: number;
  columnWidth: number;
  columnCount: number;
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
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

class DiscoverGamePosters extends Component<
  DiscoverGamePostersProps,
  DiscoverGamePostersState
> {
  _isInitialized = false;
  _grid: Grid | null = null;
  _padding: number;

  //
  // Lifecycle

  constructor(props: DiscoverGamePostersProps) {
    super(props);

    this.state = {
      width: 0,
      columnWidth: 182,
      columnCount: 1,
      posterWidth: 162,
      posterHeight: 238,
      rowHeight: calculateRowHeight(
        238,
        undefined,
        props.isSmallScreen,
        {} as PosterOptions
      ),
    };

    this._padding = props.isSmallScreen
      ? columnPaddingSmallScreen
      : columnPadding;
  }

  componentDidUpdate(
    prevProps: DiscoverGamePostersProps,
    prevState: DiscoverGamePostersState
  ) {
    const {
      items,
      sortKey,
      posterOptions,
      jumpToCharacter,
      isSmallScreen,
      selectedState,
    } = this.props;

    const { width, columnWidth, columnCount, rowHeight } = this.state;

    if (
      prevProps.sortKey !== sortKey ||
      prevProps.posterOptions !== posterOptions
    ) {
      this.calculateGrid(width, isSmallScreen);
    }

    if (
      this._grid &&
      (prevState.width !== width ||
        prevState.columnWidth !== columnWidth ||
        prevState.columnCount !== columnCount ||
        prevState.rowHeight !== rowHeight ||
        prevProps.selectedState !== selectedState ||
        hasDifferentItemsOrOrder(prevProps.items, items, 'igdbId'))
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
        const row = Math.floor(index / columnCount);

        this._gridScrollToCell({
          rowIndex: row,
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
    const { sortKey, posterOptions } = this.props;

    const columnWidth = calculateColumnWidth(
      width,
      posterOptions.size,
      isSmallScreen
    );
    const columnCount = Math.max(Math.floor(width / columnWidth), 1);
    const posterWidth = columnWidth - this._padding * 2;
    const posterHeight = calculatePosterHeight(posterWidth);
    const rowHeight = calculateRowHeight(
      posterHeight,
      sortKey,
      isSmallScreen,
      posterOptions
    );

    this.setState({
      width,
      columnWidth,
      columnCount,
      posterWidth,
      posterHeight,
      rowHeight,
    });
  };

  cellRenderer: GridCellRenderer = ({ key, rowIndex, columnIndex, style }) => {
    const {
      items,
      sortKey,
      posterOptions,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      selectedState,
      onSelectedChange,
    } = this.props;

    const { posterWidth, posterHeight, columnCount } = this.state;

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
          padding: this._padding,
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

    const { width, columnWidth, columnCount, rowHeight } = this.state;

    const rowCount = Math.ceil(items.length / columnCount);

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
                  columnCount={columnCount}
                  columnWidth={columnWidth}
                  rowCount={rowCount}
                  rowHeight={rowHeight}
                  width={width}
                  scrollTop={scrollTop}
                  overscanRowCount={2}
                  cellRenderer={this.cellRenderer}
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
}

export default DiscoverGamePosters;
