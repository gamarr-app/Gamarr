import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import styles from 'Game/Index/GameIndex.css';
import { align, icons, kinds, sortDirections } from 'Helpers/Props';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import DiscoverGameFooterConnector from './DiscoverGameFooterConnector';
import DiscoverGameFilterMenu from './Menus/DiscoverGameFilterMenu';
import DiscoverGameSortMenu from './Menus/DiscoverGameSortMenu';
import DiscoverGameViewMenu from './Menus/DiscoverGameViewMenu';
import NoDiscoverGame from './NoDiscoverGame';
import DiscoverGameOverviewsConnector from './Overview/DiscoverGameOverviewsConnector';
import DiscoverGameOverviewOptionsModal from './Overview/Options/DiscoverGameOverviewOptionsModal';
import DiscoverGamePostersConnector from './Posters/DiscoverGamePostersConnector';
import DiscoverGamePosterOptionsModal from './Posters/Options/DiscoverGamePosterOptionsModal';
import DiscoverGameTableConnector from './Table/DiscoverGameTableConnector';
import DiscoverGameTableOptionsConnector from './Table/DiscoverGameTableOptionsConnector';

function getViewComponent(view) {
  if (view === 'posters') {
    return DiscoverGamePostersConnector;
  }

  if (view === 'overview') {
    return DiscoverGameOverviewsConnector;
  }

  return DiscoverGameTableConnector;
}

class DiscoverGame extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.scrollerRef = React.createRef();

    this.state = {
      jumpBarItems: { order: [] },
      jumpToCharacter: null,
      isPosterOptionsModalOpen: false,
      isOverviewOptionsModalOpen: false,
      isConfirmSearchModalOpen: false,
      searchType: null,
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {}
    };
  }

  componentDidMount() {
    this.setJumpBarItems();
    this.setSelectedState();
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection,
      includeRecommendations,
      includeTrending,
      includePopular
    } = this.props;

    if (includeRecommendations !== prevProps.includeRecommendations ||
      includeTrending !== prevProps.includeTrending ||
      includePopular !== prevProps.includePopular
    ) {
      this.props.dispatchFetchListGames();
    }

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      this.setJumpBarItems();
      this.setSelectedState();
    }

    if (this.state.jumpToCharacter != null) {
      this.setState({ jumpToCharacter: null });
    }
  }

  //
  // Control

  getSelectedIds = () => {
    if (this.state.allUnselected) {
      return [];
    }
    return getSelectedIds(this.state.selectedState);
  };

  setSelectedState() {
    const {
      items
    } = this.props;

    const {
      selectedState
    } = this.state;

    const newSelectedState = {};

    items.forEach((game) => {
      const isItemSelected = selectedState[game.igdbId];

      if (isItemSelected) {
        newSelectedState[game.igdbId] = isItemSelected;
      } else {
        newSelectedState[game.igdbId] = false;
      }
    });

    const selectedCount = getSelectedIds(newSelectedState).length;
    const newStateCount = Object.keys(newSelectedState).length;
    let isAllSelected = false;
    let isAllUnselected = false;

    if (selectedCount === 0) {
      isAllUnselected = true;
    } else if (selectedCount === newStateCount) {
      isAllSelected = true;
    }

    this.setState({ selectedState: newSelectedState, allSelected: isAllSelected, allUnselected: isAllUnselected });
  }

  setJumpBarItems() {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    // Reset if not sorting by sortTitle
    if (sortKey !== 'sortTitle') {
      this.setState({ jumpBarItems: { order: [] } });
      return;
    }

    const characters = _.reduce(items, (acc, item) => {
      let char = item.sortTitle.charAt(0);

      if (!isNaN(char)) {
        char = '#';
      }

      if (char in acc) {
        acc[char] = acc[char] + 1;
      } else {
        acc[char] = 1;
      }

      return acc;
    }, {});

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === sortDirections.DESCENDING) {
      order.reverse();
    }

    const jumpBarItems = {
      characters,
      order
    };

    this.setState({ jumpBarItems });
  }

  //
  // Listeners

  onPosterOptionsPress = () => {
    this.setState({ isPosterOptionsModalOpen: true });
  };

  onPosterOptionsModalClose = () => {
    this.setState({ isPosterOptionsModalOpen: false });
  };

  onOverviewOptionsPress = () => {
    this.setState({ isOverviewOptionsModalOpen: true });
  };

  onOverviewOptionsModalClose = () => {
    this.setState({ isOverviewOptionsModalOpen: false });
  };

  onJumpBarItemPress = (jumpToCharacter) => {
    this.setState({ jumpToCharacter });
  };

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  };

  onImportListSyncPress = () => {
    this.props.onImportListSyncPress();
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey, 'igdbId');
    });
  };

  onAddGamesPress = ({ addOptions }) => {
    this.props.onAddGamesPress({ ids: this.getSelectedIds(), addOptions });
  };

  onExcludeGamesPress = () => {
    this.props.onExcludeGamesPress({ ids: this.getSelectedIds() });
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      totalItems,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      view,
      onSortSelect,
      onFilterSelect,
      onViewSelect,
      initialScrollTop,
      onScroll,
      onAddGamesPress,
      isSyncingLists,
      ...otherProps
    } = this.props;

    const {
      jumpBarItems,
      jumpToCharacter,
      isPosterOptionsModalOpen,
      isOverviewOptionsModalOpen,
      selectedState,
      allSelected,
      allUnselected
    } = this.state;

    const selectedGameIds = this.getSelectedIds();

    const ViewComponent = getViewComponent(view);
    const isLoaded = !!(!error && isPopulated && items.length && this.scrollerRef.current);
    const hasNoGame = !totalItems;

    return (
      <PageContent title={translate('Discover')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('RefreshLists')}
              iconName={icons.REFRESH}
              isSpinning={isSyncingLists}
              isDisabled={hasNoGame}
              onPress={this.onImportListSyncPress}
            />
            <PageToolbarButton
              label={allSelected ? translate('UnselectAll') : translate('SelectAll')}
              iconName={icons.CHECK_SQUARE}
              isDisabled={hasNoGame}
              onPress={this.onSelectAllPress}
            />
          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            {
              view === 'table' ?
                <TableOptionsModalWrapper
                  {...otherProps}
                  columns={columns}
                  optionsComponent={DiscoverGameTableOptionsConnector}
                >
                  <PageToolbarButton
                    label={translate('Options')}
                    iconName={icons.TABLE}
                  />
                </TableOptionsModalWrapper> :
                null
            }

            {
              view === 'posters' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.POSTER}
                  onPress={this.onPosterOptionsPress}
                /> :
                null
            }

            {
              view === 'overview' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.OVERVIEW}
                  onPress={this.onOverviewOptionsPress}
                /> :
                null
            }

            <PageToolbarSeparator />

            <DiscoverGameViewMenu
              view={view}
              isDisabled={hasNoGame}
              onViewSelect={onViewSelect}
            />

            <DiscoverGameSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoGame}
              onSortSelect={onSortSelect}
            />

            <DiscoverGameFilterMenu
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              isDisabled={hasNoGame}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <div className={styles.pageContentBodyWrapper}>
          <PageContentBody
            ref={this.scrollerRef}
            className={styles.contentBody}
            innerClassName={styles[`${view}InnerContentBody`]}
            onScroll={onScroll}
          >
            {
              isFetching && !isPopulated &&
                <LoadingIndicator />
            }

            {
              !isFetching && !!error &&
                <Alert kind={kinds.DANGER}>
                  {translate('UnableToLoadGames')}
                </Alert>
            }

            {
              isLoaded &&
                <div className={styles.contentBodyContainer}>
                  <ViewComponent
                    scroller={this.scrollerRef.current}
                    items={items}
                    filters={filters}
                    sortKey={sortKey}
                    sortDirection={sortDirection}
                    jumpToCharacter={jumpToCharacter}
                    allSelected={allSelected}
                    allUnselected={allUnselected}
                    onSelectedChange={this.onSelectedChange}
                    onSelectAllChange={this.onSelectAllChange}
                    selectedState={selectedState}
                    scrollTop={initialScrollTop}
                    {...otherProps}
                  />
                </div>
            }

            {
              !error && isPopulated && !items.length &&
                <NoDiscoverGame totalItems={totalItems} />
            }
          </PageContentBody>

          {
            isLoaded && !!jumpBarItems.order.length &&
              <PageJumpBar
                items={jumpBarItems}
                onItemPress={this.onJumpBarItemPress}
              />
          }
        </div>

        {
          isLoaded &&
            <DiscoverGameFooterConnector
              selectedIds={selectedGameIds}
              onAddGamesPress={this.onAddGamesPress}
              onExcludeGamesPress={this.onExcludeGamesPress}
            />
        }

        <DiscoverGamePosterOptionsModal
          isOpen={isPosterOptionsModalOpen}
          onModalClose={this.onPosterOptionsModalClose}
        />

        <DiscoverGameOverviewOptionsModal
          isOpen={isOverviewOptionsModalOpen}
          onModalClose={this.onOverviewOptionsModalClose}
        />
      </PageContent>
    );
  }
}

DiscoverGame.propTypes = {
  initialScrollTop: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  totalItems: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  view: PropTypes.string.isRequired,
  includeRecommendations: PropTypes.bool.isRequired,
  includeTrending: PropTypes.bool.isRequired,
  includePopular: PropTypes.bool.isRequired,
  isSyncingLists: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onSortSelect: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onViewSelect: PropTypes.func.isRequired,
  onScroll: PropTypes.func.isRequired,
  onAddGamesPress: PropTypes.func.isRequired,
  onExcludeGamesPress: PropTypes.func.isRequired,
  onImportListSyncPress: PropTypes.func.isRequired,
  dispatchFetchListGames: PropTypes.func.isRequired
};

export default DiscoverGame;
