import _ from 'lodash';
import { ComponentType, useCallback, useEffect, useRef, useState } from 'react';
import { CustomFilter, Filter } from 'App/State/AppState';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar, { PageJumpBarItems } from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import Column from 'Components/Table/Column';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import styles from 'Game/Index/GameIndex.css';
import { align, icons, kinds, sortDirections } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import { SelectedState } from 'Utilities/Table/areAllSelected';
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

interface DiscoverGameItem {
  id: number;
  igdbId: number;
  sortTitle: string;
  [key: string]: unknown;
}

interface DiscoverGameProps {
  initialScrollTop?: number;
  isFetching: boolean;
  isPopulated: boolean;
  error?: object | null;
  totalItems: number;
  items: DiscoverGameItem[];
  columns: Column[];
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  sortKey?: string;
  sortDirection?: SortDirection;
  view: string;
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
  isSyncingLists: boolean;
  isSmallScreen: boolean;
  onSortSelect: (sortKey: string) => void;
  onFilterSelect: (filterKey: string | number) => void;
  onViewSelect: (view: string) => void;
  onScroll: (options: { scrollTop: number }) => void;
  onAddGamesPress: (options: {
    ids: number[];
    addOptions: Record<string, unknown>;
  }) => void;
  onExcludeGamesPress: (options: { ids: number[] }) => void;
  onImportListSyncPress: () => void;
  dispatchFetchListGames: () => void;
  onTableOptionChange?: (options: {
    pageSize?: number;
    columns?: Column[];
  }) => void;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- View components have varying props
type ViewComponent = ComponentType<any>;

function getViewComponent(view: string): ViewComponent {
  if (view === 'posters') {
    return DiscoverGamePostersConnector;
  }

  if (view === 'overview') {
    return DiscoverGameOverviewsConnector;
  }

  return DiscoverGameTableConnector;
}

function DiscoverGame({
  initialScrollTop,
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
  includeRecommendations,
  includeTrending,
  includePopular,
  isSyncingLists,
  isSmallScreen,
  onSortSelect,
  onFilterSelect,
  onViewSelect,
  onScroll,
  onAddGamesPress,
  onExcludeGamesPress,
  onImportListSyncPress,
  dispatchFetchListGames,
  onTableOptionChange,
}: DiscoverGameProps) {
  const scrollerRef = useRef<HTMLDivElement>(null);

  const [jumpBarItems, setJumpBarItems] = useState<PageJumpBarItems>({
    order: [],
    characters: {},
  });
  const [jumpToCharacter, setJumpToCharacter] = useState<string | undefined>(
    undefined
  );
  const [isPosterOptionsModalOpen, setIsPosterOptionsModalOpen] =
    useState(false);
  const [isOverviewOptionsModalOpen, setIsOverviewOptionsModalOpen] =
    useState(false);
  const [allSelected, setAllSelected] = useState(false);
  const [allUnselected, setAllUnselected] = useState(false);
  const [lastToggled, setLastToggled] = useState<number | string | null>(null);
  const [selectedState, setSelectedState] = useState<SelectedState>({});

  const prevIncludeRecommendations = useRef(includeRecommendations);
  const prevIncludeTrending = useRef(includeTrending);
  const prevIncludePopular = useRef(includePopular);
  const prevSortKey = useRef(sortKey);
  const prevSortDirection = useRef(sortDirection);
  const prevItems = useRef(items);

  const getSelectedIdsList = useCallback(() => {
    if (allUnselected) {
      return [];
    }
    const ids: number[] = [];
    Object.entries(selectedState).forEach(([key, value]) => {
      if (value) {
        ids.push(parseInt(key));
      }
    });
    return ids;
  }, [allUnselected, selectedState]);

  const setSelectedStateFromItems = useCallback(() => {
    const newSelectedState: SelectedState = {};

    items.forEach((game) => {
      const isItemSelected = selectedState[game.igdbId];

      if (isItemSelected) {
        newSelectedState[game.igdbId] = isItemSelected;
      } else {
        newSelectedState[game.igdbId] = false;
      }
    });

    const selectedCount =
      Object.values(newSelectedState).filter(Boolean).length;
    const newStateCount = Object.keys(newSelectedState).length;
    let isAllSelected = false;
    let isAllUnselected = false;

    if (selectedCount === 0) {
      isAllUnselected = true;
    } else if (selectedCount === newStateCount) {
      isAllSelected = true;
    }

    setSelectedState(newSelectedState);
    setAllSelected(isAllSelected);
    setAllUnselected(isAllUnselected);
  }, [items, selectedState]);

  const setJumpBarItemsFromItems = useCallback(() => {
    // Reset if not sorting by sortTitle
    if (sortKey !== 'sortTitle') {
      setJumpBarItems({ order: [], characters: {} });
      return;
    }

    const characters = _.reduce(
      items,
      (acc: Record<string, number>, item) => {
        let char = item.sortTitle.charAt(0);

        if (!isNaN(parseInt(char))) {
          char = '#';
        }

        if (char in acc) {
          acc[char] = acc[char] + 1;
        } else {
          acc[char] = 1;
        }

        return acc;
      },
      {}
    );

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === sortDirections.DESCENDING) {
      order.reverse();
    }

    setJumpBarItems({
      characters,
      order,
    });
  }, [items, sortKey, sortDirection]);

  useEffect(() => {
    setJumpBarItemsFromItems();
    setSelectedStateFromItems();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (
      includeRecommendations !== prevIncludeRecommendations.current ||
      includeTrending !== prevIncludeTrending.current ||
      includePopular !== prevIncludePopular.current
    ) {
      dispatchFetchListGames();
    }

    prevIncludeRecommendations.current = includeRecommendations;
    prevIncludeTrending.current = includeTrending;
    prevIncludePopular.current = includePopular;
  }, [
    includeRecommendations,
    includeTrending,
    includePopular,
    dispatchFetchListGames,
  ]);

  useEffect(() => {
    if (
      sortKey !== prevSortKey.current ||
      sortDirection !== prevSortDirection.current ||
      hasDifferentItemsOrOrder(prevItems.current, items, 'igdbId')
    ) {
      setJumpBarItemsFromItems();
      setSelectedStateFromItems();
    }

    prevSortKey.current = sortKey;
    prevSortDirection.current = sortDirection;
    prevItems.current = items;
  }, [
    items,
    sortKey,
    sortDirection,
    setJumpBarItemsFromItems,
    setSelectedStateFromItems,
  ]);

  useEffect(() => {
    if (jumpToCharacter != null) {
      setJumpToCharacter(undefined);
    }
  }, [jumpToCharacter]);

  const handlePosterOptionsPress = useCallback(() => {
    setIsPosterOptionsModalOpen(true);
  }, []);

  const handlePosterOptionsModalClose = useCallback(() => {
    setIsPosterOptionsModalOpen(false);
  }, []);

  const handleOverviewOptionsPress = useCallback(() => {
    setIsOverviewOptionsModalOpen(true);
  }, []);

  const handleOverviewOptionsModalClose = useCallback(() => {
    setIsOverviewOptionsModalOpen(false);
  }, []);

  const handleJumpBarItemPress = useCallback((character: string) => {
    setJumpToCharacter(character);
  }, []);

  const handleSelectAllChange = useCallback(
    ({ value }: { value: boolean }) => {
      const result = selectAll(selectedState, value);
      setSelectedState(result.selectedState);
      setAllSelected(result.allSelected);
      setAllUnselected(result.allUnselected);
    },
    [selectedState]
  );

  const handleSelectAllPress = useCallback(() => {
    handleSelectAllChange({ value: !allSelected });
  }, [allSelected, handleSelectAllChange]);

  const handleImportListSyncPress = useCallback(() => {
    onImportListSyncPress();
  }, [onImportListSyncPress]);

  const handleSelectedChange = useCallback(
    ({
      id,
      value,
      shiftKey = false,
    }: {
      id: number | string;
      value: boolean | null;
      shiftKey: boolean;
    }) => {
      const result = toggleSelected(
        { selectedState, lastToggled, allSelected, allUnselected },
        items,
        id,
        value,
        shiftKey
      );
      setSelectedState(result.selectedState);
      setAllSelected(result.allSelected);
      setAllUnselected(result.allUnselected);
      setLastToggled(result.lastToggled);
    },
    [selectedState, lastToggled, allSelected, allUnselected, items]
  );

  const handleAddGamesPress = useCallback(
    ({ addOptions }: { addOptions: Record<string, unknown> }) => {
      onAddGamesPress({ ids: getSelectedIdsList(), addOptions });
    },
    [getSelectedIdsList, onAddGamesPress]
  );

  const handleExcludeGamesPress = useCallback(() => {
    onExcludeGamesPress({ ids: getSelectedIdsList() });
  }, [getSelectedIdsList, onExcludeGamesPress]);

  const selectedGameIds = getSelectedIdsList();
  const ViewComponent = getViewComponent(view);
  const isLoaded = !!(
    !error &&
    isPopulated &&
    items.length &&
    scrollerRef.current
  );
  const hasNoGame = !totalItems;

  const innerClassName =
    view === 'posters'
      ? styles.postersInnerContentBody
      : styles.tableInnerContentBody;

  return (
    <PageContent title={translate('Discover')}>
      <PageToolbar>
        <PageToolbarSection>
          <PageToolbarButton
            label={translate('RefreshLists')}
            iconName={icons.REFRESH}
            isSpinning={isSyncingLists}
            isDisabled={hasNoGame}
            onPress={handleImportListSyncPress}
          />
          <PageToolbarButton
            label={
              allSelected ? translate('UnselectAll') : translate('SelectAll')
            }
            iconName={icons.CHECK_SQUARE}
            isDisabled={hasNoGame}
            onPress={handleSelectAllPress}
          />
        </PageToolbarSection>

        <PageToolbarSection alignContent={align.RIGHT} collapseButtons={false}>
          {view === 'table' && onTableOptionChange ? (
            <TableOptionsModalWrapper
              columns={columns}
              optionsComponent={DiscoverGameTableOptionsConnector}
              onTableOptionChange={onTableOptionChange}
            >
              <PageToolbarButton
                label={translate('Options')}
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper>
          ) : null}

          {view === 'posters' ? (
            <PageToolbarButton
              label={translate('Options')}
              iconName={icons.POSTER}
              onPress={handlePosterOptionsPress}
            />
          ) : null}

          {view === 'overview' ? (
            <PageToolbarButton
              label={translate('Options')}
              iconName={icons.OVERVIEW}
              onPress={handleOverviewOptionsPress}
            />
          ) : null}

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
          ref={scrollerRef}
          className={styles.contentBody}
          innerClassName={innerClassName}
          onScroll={onScroll}
        >
          {isFetching && !isPopulated && <LoadingIndicator />}

          {!isFetching && !!error && (
            <Alert kind={kinds.DANGER}>{translate('UnableToLoadGames')}</Alert>
          )}

          {isLoaded && (
            <div className={styles.contentBodyContainer}>
              <ViewComponent
                scroller={scrollerRef.current}
                items={items}
                filters={filters}
                sortKey={sortKey}
                sortDirection={sortDirection}
                jumpToCharacter={jumpToCharacter}
                allSelected={allSelected}
                allUnselected={allUnselected}
                selectedState={selectedState}
                scrollTop={initialScrollTop}
                isSmallScreen={isSmallScreen}
                onSelectedChange={handleSelectedChange}
                onSelectAllChange={handleSelectAllChange}
              />
            </div>
          )}

          {!error && isPopulated && !items.length && (
            <NoDiscoverGame totalItems={totalItems} />
          )}
        </PageContentBody>

        {isLoaded && !!jumpBarItems.order.length && (
          <PageJumpBar
            items={jumpBarItems}
            onItemPress={handleJumpBarItemPress}
          />
        )}
      </div>

      {isLoaded && (
        <DiscoverGameFooterConnector
          selectedIds={selectedGameIds}
          onAddGamesPress={handleAddGamesPress}
          onExcludeGamesPress={handleExcludeGamesPress}
        />
      )}

      <DiscoverGamePosterOptionsModal
        isOpen={isPosterOptionsModalOpen}
        onModalClose={handlePosterOptionsModalClose}
      />

      <DiscoverGameOverviewOptionsModal
        isOpen={isOverviewOptionsModalOpen}
        onModalClose={handleOverviewOptionsModalClose}
      />
    </PageContent>
  );
}

export default DiscoverGame;
