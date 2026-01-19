import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useHistory } from 'react-router';
import { SelectProvider } from 'App/SelectContext';
import ClientSideCollectionAppState from 'App/State/ClientSideCollectionAppState';
import GamesAppState, { GameIndexAppState } from 'App/State/GamesAppState';
import { RSS_SYNC } from 'Commands/commandNames';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar, { PageJumpBarItems } from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import withScrollPosition from 'Components/withScrollPosition';
import { align, icons, kinds } from 'Helpers/Props';
import { DESCENDING } from 'Helpers/Props/sortDirections';
import InteractiveImportModal from 'InteractiveImport/InteractiveImportModal';
import NoGame from 'Game/NoGame';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchGames } from 'Store/Actions/gameActions';
import {
  setGameFilter,
  setGameSort,
  setGameTableOption,
  setGameView,
} from 'Store/Actions/gameIndexActions';
import { fetchQueueDetails } from 'Store/Actions/queueActions';
import scrollPositions from 'Store/scrollPositions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createGameClientSideCollectionItemsSelector from 'Store/Selectors/createGameClientSideCollectionItemsSelector';
import translate from 'Utilities/String/translate';
import GameIndexFilterMenu from './Menus/GameIndexFilterMenu';
import GameIndexSortMenu from './Menus/GameIndexSortMenu';
import GameIndexViewMenu from './Menus/GameIndexViewMenu';
import GameIndexFooter from './GameIndexFooter';
import GameIndexRefreshGameButton from './GameIndexRefreshGameButton';
import GameIndexSearchButton from './GameIndexSearchButton';
import GameIndexSearchMenuItem from './GameIndexSearchMenuItem';
import GameIndexOverviews from './Overview/GameIndexOverviews';
import GameIndexOverviewOptionsModal from './Overview/Options/GameIndexOverviewOptionsModal';
import GameIndexPosters from './Posters/GameIndexPosters';
import GameIndexPosterOptionsModal from './Posters/Options/GameIndexPosterOptionsModal';
import GameIndexSelectAllButton from './Select/GameIndexSelectAllButton';
import GameIndexSelectAllMenuItem from './Select/GameIndexSelectAllMenuItem';
import GameIndexSelectFooter from './Select/GameIndexSelectFooter';
import GameIndexSelectModeButton from './Select/GameIndexSelectModeButton';
import GameIndexSelectModeMenuItem from './Select/GameIndexSelectModeMenuItem';
import GameIndexTable from './Table/GameIndexTable';
import GameIndexTableOptions from './Table/GameIndexTableOptions';
import styles from './GameIndex.css';

function getViewComponent(view: string) {
  if (view === 'posters') {
    return GameIndexPosters;
  }

  if (view === 'overview') {
    return GameIndexOverviews;
  }

  return GameIndexTable;
}

interface GameIndexProps {
  initialScrollTop?: number;
}

const GameIndex = withScrollPosition((props: GameIndexProps) => {
  const history = useHistory();

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
  }: GamesAppState & GameIndexAppState & ClientSideCollectionAppState =
    useSelector(createGameClientSideCollectionItemsSelector('gameIndex'));

  const isRssSyncExecuting = useSelector(
    createCommandExecutingSelector(RSS_SYNC)
  );
  const { isSmallScreen } = useSelector(createDimensionsSelector());
  const dispatch = useDispatch();
  const scrollerRef = useRef<HTMLDivElement>(null);
  const [isOptionsModalOpen, setIsOptionsModalOpen] = useState(false);
  const [isInteractiveImportModalOpen, setIsInteractiveImportModalOpen] =
    useState(false);
  const [jumpToCharacter, setJumpToCharacter] = useState<string | undefined>(
    undefined
  );
  const [isSelectMode, setIsSelectMode] = useState(false);

  useEffect(() => {
    if (history.action === 'PUSH') {
      dispatch(fetchGames());
    }
  }, [history, dispatch]);

  useEffect(() => {
    dispatch(fetchQueueDetails({ all: true }));
  }, [dispatch]);

  const onRssSyncPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: RSS_SYNC,
      })
    );
  }, [dispatch]);

  const onSelectModePress = useCallback(() => {
    setIsSelectMode(!isSelectMode);
  }, [isSelectMode, setIsSelectMode]);

  const onTableOptionChange = useCallback(
    (payload: unknown) => {
      dispatch(setGameTableOption(payload));
    },
    [dispatch]
  );

  const onViewSelect = useCallback(
    (value: string) => {
      dispatch(setGameView({ view: value }));

      if (scrollerRef.current) {
        scrollerRef.current.scrollTo(0, 0);
      }
    },
    [scrollerRef, dispatch]
  );

  const onSortSelect = useCallback(
    (value: string) => {
      dispatch(setGameSort({ sortKey: value }));
    },
    [dispatch]
  );

  const onFilterSelect = useCallback(
    (value: string | number) => {
      dispatch(setGameFilter({ selectedFilterKey: value }));
    },
    [dispatch]
  );

  const onOptionsPress = useCallback(() => {
    setIsOptionsModalOpen(true);
  }, [setIsOptionsModalOpen]);

  const onOptionsModalClose = useCallback(() => {
    setIsOptionsModalOpen(false);
  }, [setIsOptionsModalOpen]);

  const onInteractiveImportPress = useCallback(() => {
    setIsInteractiveImportModalOpen(true);
  }, [setIsInteractiveImportModalOpen]);

  const onInteractiveImportModalClose = useCallback(() => {
    setIsInteractiveImportModalOpen(false);
  }, [setIsInteractiveImportModalOpen]);

  const onJumpBarItemPress = useCallback(
    (character: string) => {
      setJumpToCharacter(character);
    },
    [setJumpToCharacter]
  );

  const onScroll = useCallback(
    ({ scrollTop }: { scrollTop: number }) => {
      setJumpToCharacter(undefined);
      scrollPositions.gameIndex = scrollTop;
    },
    [setJumpToCharacter]
  );

  const jumpBarItems: PageJumpBarItems = useMemo(() => {
    // Reset if not sorting by sortTitle
    if (sortKey !== 'sortTitle') {
      return {
        characters: {},
        order: [],
      };
    }

    const characters = items.reduce((acc: Record<string, number>, item) => {
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
    }, {});

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === DESCENDING) {
      order.reverse();
    }

    return {
      characters,
      order,
    };
  }, [items, sortKey, sortDirection]);
  const ViewComponent = useMemo(() => getViewComponent(view), [view]);

  const isLoaded = !!(!error && isPopulated && items.length);
  const hasNoGame = !totalItems;

  return (
    <SelectProvider items={items}>
      <PageContent>
        <PageToolbar>
          <PageToolbarSection>
            <GameIndexRefreshGameButton
              isSelectMode={isSelectMode}
              selectedFilterKey={selectedFilterKey}
            />

            <PageToolbarButton
              label={translate('RssSync')}
              iconName={icons.RSS}
              isSpinning={isRssSyncExecuting}
              isDisabled={hasNoGame}
              onPress={onRssSyncPress}
            />

            <PageToolbarSeparator />

            <GameIndexSearchButton
              isSelectMode={isSelectMode}
              selectedFilterKey={selectedFilterKey}
              overflowComponent={GameIndexSearchMenuItem}
            />

            <PageToolbarButton
              label={translate('ManualImport')}
              iconName={icons.INTERACTIVE}
              isDisabled={hasNoGame}
              onPress={onInteractiveImportPress}
            />

            <PageToolbarSeparator />

            <GameIndexSelectModeButton
              label={
                isSelectMode
                  ? translate('StopSelecting')
                  : translate('EditGames')
              }
              iconName={isSelectMode ? icons.SERIES_ENDED : icons.EDIT}
              isSelectMode={isSelectMode}
              overflowComponent={GameIndexSelectModeMenuItem}
              onPress={onSelectModePress}
            />

            <GameIndexSelectAllButton
              label={translate('SelectAll')}
              isSelectMode={isSelectMode}
              overflowComponent={GameIndexSelectAllMenuItem}
            />
          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            {view === 'table' ? (
              <TableOptionsModalWrapper
                columns={columns}
                optionsComponent={GameIndexTableOptions}
                onTableOptionChange={onTableOptionChange}
              >
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.TABLE}
                />
              </TableOptionsModalWrapper>
            ) : (
              <PageToolbarButton
                label={translate('Options')}
                iconName={view === 'posters' ? icons.POSTER : icons.OVERVIEW}
                isDisabled={hasNoGame}
                onPress={onOptionsPress}
              />
            )}

            <PageToolbarSeparator />

            <GameIndexViewMenu
              view={view}
              isDisabled={hasNoGame}
              onViewSelect={onViewSelect}
            />

            <GameIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoGame}
              onSortSelect={onSortSelect}
            />

            <GameIndexFilterMenu
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
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            innerClassName={styles[`${view}InnerContentBody`]}
            initialScrollTop={props.initialScrollTop}
            onScroll={onScroll}
          >
            {isFetching && !isPopulated ? <LoadingIndicator /> : null}

            {!isFetching && !!error ? (
              <Alert kind={kinds.DANGER}>
                {translate('UnableToLoadGames')}
              </Alert>
            ) : null}

            {isLoaded ? (
              <div className={styles.contentBodyContainer}>
                <ViewComponent
                  scrollerRef={scrollerRef}
                  items={items}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  jumpToCharacter={jumpToCharacter}
                  isSelectMode={isSelectMode}
                  isSmallScreen={isSmallScreen}
                />

                <GameIndexFooter />
              </div>
            ) : null}

            {!error && isPopulated && !items.length ? (
              <NoGame totalItems={totalItems} />
            ) : null}
          </PageContentBody>

          {isLoaded && !!jumpBarItems.order.length ? (
            <PageJumpBar
              items={jumpBarItems}
              onItemPress={onJumpBarItemPress}
            />
          ) : null}
        </div>

        {isSelectMode ? <GameIndexSelectFooter /> : null}

        <InteractiveImportModal
          isOpen={isInteractiveImportModalOpen}
          onModalClose={onInteractiveImportModalClose}
        />

        {view === 'posters' ? (
          <GameIndexPosterOptionsModal
            isOpen={isOptionsModalOpen}
            onModalClose={onOptionsModalClose}
          />
        ) : null}
        {view === 'overview' ? (
          <GameIndexOverviewOptionsModal
            isOpen={isOptionsModalOpen}
            onModalClose={onOptionsModalClose}
          />
        ) : null}
      </PageContent>
    </SelectProvider>
  );
}, 'gameIndex');

export default GameIndex;
