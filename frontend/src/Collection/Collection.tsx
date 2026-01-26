import _ from 'lodash';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Error } from 'App/State/AppSectionState';
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
import styles from 'Game/Index/GameIndex.css';
import { align, icons, kinds, sortDirections } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import { CollectionItem } from 'Store/Selectors/createCollectionClientSideCollectionItemsSelector';
import { SelectStateInputProps } from 'typings/props';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';

type GetSelectedIdsFn = typeof getSelectedIds;
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import CollectionFooter from './CollectionFooter';
import GameCollectionFilterMenu from './Menus/GameCollectionFilterMenu';
import GameCollectionSortMenu from './Menus/GameCollectionSortMenu';
import NoGameCollections from './NoGameCollections';
import CollectionOverviewsConnector from './Overview/CollectionOverviewsConnector';
import CollectionOverviewOptionsModal from './Overview/Options/CollectionOverviewOptionsModal';

type JumpBarItems = PageJumpBarItems;

interface CollectionProps {
  initialScrollTop?: number;
  isFetching: boolean;
  isPopulated: boolean;
  isSaving: boolean;
  isAdding: boolean;
  error?: Error;
  saveError?: Error;
  totalItems: number;
  items: CollectionItem[];
  selectedFilterKey: string | number;
  filters: Filter[];
  customFilters: CustomFilter[];
  sortKey?: string;
  sortDirection?: SortDirection;
  view: string;
  isRefreshingCollections: boolean;
  isSmallScreen: boolean;
  onSortSelect: (sortKey: string) => void;
  onFilterSelect: (filterKey: string | number) => void;
  onScroll: (props: { scrollTop: number }) => void;
  onUpdateSelectedPress: (payload: {
    collectionIds: number[];
    [key: string]: unknown;
  }) => void;
  onRefreshGameCollectionsPress: () => void;
}

function getViewComponent(_view: string) {
  return CollectionOverviewsConnector;
}

function Collection(props: CollectionProps) {
  const {
    initialScrollTop,
    isFetching,
    isPopulated,
    isSaving,
    isAdding,
    error,
    saveError,
    totalItems,
    items,
    selectedFilterKey,
    filters,
    customFilters,
    sortKey,
    sortDirection,
    view,
    isRefreshingCollections,
    onSortSelect,
    onFilterSelect,
    onScroll,
    onUpdateSelectedPress,
    onRefreshGameCollectionsPress,
  } = props;

  const scrollerRef = useRef<HTMLDivElement>(null);

  const [jumpBarItems, setJumpBarItems] = useState<JumpBarItems>({
    order: [],
    characters: {},
  });
  const [jumpToCharacter, setJumpToCharacter] = useState<string | null>(null);
  const [isOverviewOptionsModalOpen, setIsOverviewOptionsModalOpen] =
    useState(false);
  const [allSelected, setAllSelected] = useState(false);
  const [allUnselected, setAllUnselected] = useState(false);
  const [lastToggled, setLastToggled] = useState<number | null>(null);
  const [selectedState, setSelectedState] = useState<Record<number, boolean>>(
    {}
  );

  const prevItemsRef = useRef(items);
  const prevSortKeyRef = useRef(sortKey);
  const prevSortDirectionRef = useRef(sortDirection);

  const computeJumpBarItems = useCallback(() => {
    // Reset if not sorting by sortTitle
    if (sortKey !== 'sortTitle') {
      setJumpBarItems({ order: [], characters: {} });
      return;
    }

    const characters = _.reduce(
      items,
      (acc: Record<string, number>, item) => {
        let char = item.sortTitle.charAt(0);

        if (!isNaN(Number(char))) {
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

    setJumpBarItems({ characters, order });
  }, [items, sortKey, sortDirection]);

  const computeSelectedState = useCallback(() => {
    const newSelectedState: Record<number, boolean> = {};

    items.forEach((collection) => {
      const isItemSelected = selectedState[collection.id];

      if (isItemSelected) {
        newSelectedState[collection.id] = isItemSelected;
      } else {
        newSelectedState[collection.id] = false;
      }
    });

    const selectedCount = (getSelectedIds as GetSelectedIdsFn)(
      newSelectedState
    ).length;
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

  // Mount effect
  useEffect(() => {
    computeJumpBarItems();
    computeSelectedState();
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Update effect
  useEffect(() => {
    if (
      sortKey !== prevSortKeyRef.current ||
      sortDirection !== prevSortDirectionRef.current ||
      hasDifferentItemsOrOrder(prevItemsRef.current, items)
    ) {
      computeJumpBarItems();
      computeSelectedState();
    }

    prevItemsRef.current = items;
    prevSortKeyRef.current = sortKey;
    prevSortDirectionRef.current = sortDirection;
  }, [
    items,
    sortKey,
    sortDirection,
    computeJumpBarItems,
    computeSelectedState,
  ]);

  // Reset jumpToCharacter after it's been used
  useEffect(() => {
    if (jumpToCharacter != null) {
      setJumpToCharacter(null);
    }
  }, [jumpToCharacter]);

  const getSelectedIdsFn = useCallback((): number[] => {
    if (allUnselected) {
      return [];
    }
    return (getSelectedIds as GetSelectedIdsFn)(selectedState);
  }, [allUnselected, selectedState]);

  const onOverviewOptionsPress = useCallback(() => {
    setIsOverviewOptionsModalOpen(true);
  }, []);

  const onOverviewOptionsModalClose = useCallback(() => {
    setIsOverviewOptionsModalOpen(false);
  }, []);

  const onJumpBarItemPress = useCallback((char: string) => {
    setJumpToCharacter(char);
  }, []);

  const onSelectAllChange = useCallback(
    ({ value }: { value: boolean }) => {
      const result = selectAll(selectedState, value);
      setAllSelected(result.allSelected);
      setAllUnselected(result.allUnselected);
      setSelectedState(result.selectedState);
    },
    [selectedState]
  );

  const onSelectAllPress = useCallback(() => {
    onSelectAllChange({ value: !allSelected });
  }, [allSelected, onSelectAllChange]);

  const onSelectedChange = useCallback(
    ({ id, value, shiftKey = false }: SelectStateInputProps) => {
      const state = {
        allSelected,
        allUnselected,
        lastToggled,
        selectedState,
      };
      const result = toggleSelected(
        state,
        items,
        id as number,
        value as boolean,
        shiftKey
      );
      setAllSelected(result.allSelected);
      setAllUnselected(result.allUnselected);
      setLastToggled(
        typeof result.lastToggled === 'number' ? result.lastToggled : null
      );
      setSelectedState(result.selectedState);
    },
    [allSelected, allUnselected, lastToggled, selectedState, items]
  );

  const handleUpdateSelectedPress = useCallback(
    (changes: Record<string, unknown>) => {
      onUpdateSelectedPress({
        collectionIds: getSelectedIdsFn(),
        ...changes,
      });
    },
    [getSelectedIdsFn, onUpdateSelectedPress]
  );

  const selectedGameIds = getSelectedIdsFn();

  const ViewComponent = getViewComponent(view);
  const isLoaded = !!(
    !error &&
    isPopulated &&
    items.length &&
    scrollerRef.current
  );
  const hasNoCollection = !totalItems;

  return (
    <PageContent title={translate('Collections')}>
      <PageToolbar>
        <PageToolbarSection>
          <PageToolbarButton
            label={translate('RefreshCollections')}
            iconName={icons.REFRESH}
            isSpinning={isRefreshingCollections}
            isDisabled={hasNoCollection}
            onPress={onRefreshGameCollectionsPress}
          />
          <PageToolbarButton
            label={
              allSelected ? translate('UnselectAll') : translate('SelectAll')
            }
            iconName={icons.CHECK_SQUARE}
            isDisabled={hasNoCollection}
            onPress={onSelectAllPress}
          />
        </PageToolbarSection>

        <PageToolbarSection alignContent={align.RIGHT} collapseButtons={false}>
          {view === 'overview' ? (
            <PageToolbarButton
              label={translate('Options')}
              iconName={icons.OVERVIEW}
              onPress={onOverviewOptionsPress}
            />
          ) : null}

          {view === 'posters' || view === 'overview' ? (
            <PageToolbarSeparator />
          ) : null}

          <GameCollectionSortMenu
            sortKey={sortKey}
            sortDirection={sortDirection}
            isDisabled={hasNoCollection}
            onSortSelect={onSortSelect}
          />

          <GameCollectionFilterMenu
            selectedFilterKey={selectedFilterKey}
            filters={filters}
            customFilters={customFilters}
            isDisabled={hasNoCollection}
            onFilterSelect={onFilterSelect}
          />
        </PageToolbarSection>
      </PageToolbar>

      <div className={styles.pageContentBodyWrapper}>
        <PageContentBody
          ref={scrollerRef}
          className={styles.contentBody}
          innerClassName={
            styles[`${view}InnerContentBody` as keyof typeof styles]
          }
          onScroll={onScroll}
        >
          {isFetching && !isPopulated && <LoadingIndicator />}

          {!isFetching && !!error && (
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadCollections')}
            </Alert>
          )}

          {isLoaded && scrollerRef.current && (
            <div className={styles.contentBodyContainer}>
              <ViewComponent
                scroller={scrollerRef.current}
                items={items}
                sortKey={sortKey}
                sortDirection={sortDirection}
                jumpToCharacter={jumpToCharacter}
                selectedState={selectedState}
                scrollTop={initialScrollTop}
                onSelectedChange={onSelectedChange}
              />
            </div>
          )}

          {!error && isPopulated && !items.length && (
            <NoGameCollections totalItems={totalItems} />
          )}
        </PageContentBody>

        {isLoaded && !!jumpBarItems.order.length && (
          <PageJumpBar items={jumpBarItems} onItemPress={onJumpBarItemPress} />
        )}
      </div>

      {isLoaded && (
        <CollectionFooter
          selectedIds={selectedGameIds}
          isSaving={isSaving}
          isAdding={isAdding}
          saveError={saveError}
          onUpdateSelectedPress={handleUpdateSelectedPress}
        />
      )}

      <CollectionOverviewOptionsModal
        isOpen={isOverviewOptionsModalOpen}
        onModalClose={onOverviewOptionsModalClose}
      />
    </PageContent>
  );
}

export default Collection;
