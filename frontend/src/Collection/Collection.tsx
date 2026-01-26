import _ from 'lodash';
import { Component, createRef } from 'react';
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

interface CollectionState {
  jumpBarItems: JumpBarItems;
  jumpToCharacter: string | null;
  isPosterOptionsModalOpen: boolean;
  isOverviewOptionsModalOpen: boolean;
  isConfirmSearchModalOpen: boolean;
  searchType: string | null;
  allSelected: boolean;
  allUnselected: boolean;
  lastToggled: number | null;
  selectedState: Record<number, boolean>;
}

function getViewComponent(_view: string) {
  return CollectionOverviewsConnector;
}

class Collection extends Component<CollectionProps, CollectionState> {
  // eslint-disable-next-line react/sort-comp
  scrollerRef = createRef<HTMLDivElement>();

  constructor(props: CollectionProps) {
    super(props);

    this.state = {
      jumpBarItems: { order: [], characters: {} },
      jumpToCharacter: null,
      isPosterOptionsModalOpen: false,
      isOverviewOptionsModalOpen: false,
      isConfirmSearchModalOpen: false,
      searchType: null,
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {},
    };
  }

  componentDidMount() {
    this.setJumpBarItems();
    this.setSelectedState();
  }

  componentDidUpdate(prevProps: CollectionProps) {
    const { items, sortKey, sortDirection } = this.props;

    if (
      sortKey !== prevProps.sortKey ||
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

  getSelectedIds = (): number[] => {
    if (this.state.allUnselected) {
      return [];
    }
    return getSelectedIds(this.state.selectedState);
  };

  setSelectedState() {
    const { items } = this.props;

    const { selectedState } = this.state;

    const newSelectedState: Record<number, boolean> = {};

    items.forEach((collection) => {
      const isItemSelected = selectedState[collection.id];

      if (isItemSelected) {
        newSelectedState[collection.id] = isItemSelected;
      } else {
        newSelectedState[collection.id] = false;
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

    this.setState({
      selectedState: newSelectedState,
      allSelected: isAllSelected,
      allUnselected: isAllUnselected,
    });
  }

  setJumpBarItems() {
    const { items, sortKey, sortDirection } = this.props;

    // Reset if not sorting by sortTitle
    if (sortKey !== 'sortTitle') {
      this.setState({ jumpBarItems: { order: [], characters: {} } });
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

    const jumpBarItems: JumpBarItems = {
      characters,
      order,
    };

    this.setState({ jumpBarItems });
  }

  //
  // Listeners

  onOverviewOptionsPress = () => {
    this.setState({ isOverviewOptionsModalOpen: true });
  };

  onOverviewOptionsModalClose = () => {
    this.setState({ isOverviewOptionsModalOpen: false });
  };

  onJumpBarItemPress = (jumpToCharacter: string) => {
    this.setState({ jumpToCharacter });
  };

  onSelectAllChange = ({ value }: { value: boolean }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  };

  onRefreshGameCollectionsPress = () => {
    this.props.onRefreshGameCollectionsPress();
  };

  onSelectedChange = ({
    id,
    value,
    shiftKey = false,
  }: SelectStateInputProps) => {
    this.setState((state) => {
      const result = toggleSelected(
        state,
        this.props.items,
        id as number,
        value as boolean,
        shiftKey
      );
      return {
        ...result,
        lastToggled:
          typeof result.lastToggled === 'number' ? result.lastToggled : null,
      };
    });
  };

  onUpdateSelectedPress = (changes: Record<string, unknown>) => {
    this.props.onUpdateSelectedPress({
      collectionIds: this.getSelectedIds(),
      ...changes,
    });
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
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      view,
      onSortSelect,
      onFilterSelect,
      initialScrollTop,
      onScroll,
      isRefreshingCollections,
      isSaving,
      isAdding,
      saveError,
    } = this.props;

    const {
      jumpBarItems,
      jumpToCharacter,
      isOverviewOptionsModalOpen,
      selectedState,
      allSelected,
    } = this.state;

    const selectedGameIds = this.getSelectedIds();

    const ViewComponent = getViewComponent(view);
    const isLoaded = !!(
      !error &&
      isPopulated &&
      items.length &&
      this.scrollerRef.current
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
              onPress={this.onRefreshGameCollectionsPress}
            />
            <PageToolbarButton
              label={
                allSelected ? translate('UnselectAll') : translate('SelectAll')
              }
              iconName={icons.CHECK_SQUARE}
              isDisabled={hasNoCollection}
              onPress={this.onSelectAllPress}
            />
          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            {view === 'overview' ? (
              <PageToolbarButton
                label={translate('Options')}
                iconName={icons.OVERVIEW}
                onPress={this.onOverviewOptionsPress}
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
            ref={this.scrollerRef}
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

            {isLoaded && this.scrollerRef.current && (
              <div className={styles.contentBodyContainer}>
                <ViewComponent
                  scroller={this.scrollerRef.current}
                  items={items}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  jumpToCharacter={jumpToCharacter}
                  selectedState={selectedState}
                  scrollTop={initialScrollTop}
                  onSelectedChange={this.onSelectedChange}
                />
              </div>
            )}

            {!error && isPopulated && !items.length && (
              <NoGameCollections totalItems={totalItems} />
            )}
          </PageContentBody>

          {isLoaded && !!jumpBarItems.order.length && (
            <PageJumpBar
              items={jumpBarItems}
              onItemPress={this.onJumpBarItemPress}
            />
          )}
        </div>

        {isLoaded && (
          <CollectionFooter
            selectedIds={selectedGameIds}
            isSaving={isSaving}
            isAdding={isAdding}
            saveError={saveError}
            onUpdateSelectedPress={this.onUpdateSelectedPress}
          />
        )}

        <CollectionOverviewOptionsModal
          isOpen={isOverviewOptionsModalOpen}
          onModalClose={this.onOverviewOptionsModalClose}
        />
      </PageContent>
    );
  }
}

export default Collection;
