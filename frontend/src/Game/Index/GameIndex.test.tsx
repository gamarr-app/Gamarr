import { render, screen } from '@testing-library/react';
import React from 'react';
import GameIndex from './GameIndex';

import '@testing-library/jest-dom';

// Mock react-router-dom
jest.mock('react-router-dom', () => ({
  useNavigationType: jest.fn(() => 'PUSH'),
}));

// Mock react-redux
jest.mock('react-redux', () => ({
  useSelector: jest.fn(),
  useDispatch: jest.fn(() => jest.fn()),
}));

// Mock the selector
jest.mock(
  'Store/Selectors/createGameClientSideCollectionItemsSelector',
  () => ({
    __esModule: true,
    default: jest.fn(() => () => ({
      isFetching: false,
      isPopulated: true,
      error: null,
      totalItems: 0,
      items: [],
      columns: [],
      selectedFilterKey: 'all',
      filters: [],
      customFilters: [],
      sortKey: 'sortTitle',
      sortDirection: 'ascending',
      view: 'table',
    })),
  })
);

jest.mock('Store/Selectors/createCommandExecutingSelector', () => ({
  __esModule: true,
  default: jest.fn(() => () => false),
}));

jest.mock('Store/Selectors/createDimensionsSelector', () => ({
  __esModule: true,
  default: jest.fn(() => () => ({ isSmallScreen: false })),
}));

// Mock actions
jest.mock('Store/Actions/gameActions', () => ({
  fetchGames: jest.fn(() => ({ type: 'FETCH_GAMES' })),
}));

jest.mock('Store/Actions/queueActions', () => ({
  fetchQueueDetails: jest.fn(() => ({ type: 'FETCH_QUEUE_DETAILS' })),
}));

jest.mock('Store/Actions/gameIndexActions', () => ({
  setGameFilter: jest.fn(),
  setGameSort: jest.fn(),
  setGameTableOption: jest.fn(),
  setGameView: jest.fn(),
}));

jest.mock('Store/Actions/commandActions', () => ({
  executeCommand: jest.fn(),
}));

// Mock scroll positions
jest.mock('Store/scrollPositions', () => ({
  __esModule: true,
  default: { gameIndex: 0 },
}));

// Mock translate
jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

// Mock components
jest.mock('Components/Loading/LoadingIndicator', () => ({
  __esModule: true,
  default: () => <div data-testid="loading-indicator">Loading...</div>,
}));

jest.mock('Components/Alert', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="alert">{children}</div>
  ),
}));

jest.mock('Game/NoGame', () => ({
  __esModule: true,
  default: () => <div data-testid="no-game">No games found</div>,
}));

jest.mock('Components/Page/PageContent', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="page-content">{children}</div>
  ),
}));

jest.mock('Components/Page/PageContentBody', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="page-content-body">{children}</div>
  ),
}));

jest.mock('Components/Page/Toolbar/PageToolbar', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="page-toolbar">{children}</div>
  ),
}));

jest.mock('Components/Page/Toolbar/PageToolbarSection', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="page-toolbar-section">{children}</div>
  ),
}));

jest.mock('Components/Page/Toolbar/PageToolbarButton', () => ({
  __esModule: true,
  default: ({ label }: { label: string }) => (
    <button data-testid="page-toolbar-button">{label}</button>
  ),
}));

jest.mock('Components/Page/Toolbar/PageToolbarSeparator', () => ({
  __esModule: true,
  default: () => <div data-testid="page-toolbar-separator" />,
}));

jest.mock('Components/Page/PageJumpBar', () => ({
  __esModule: true,
  default: () => <div data-testid="page-jump-bar" />,
}));

jest.mock('Components/Table/TableOptions/TableOptionsModalWrapper', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="table-options-modal-wrapper">{children}</div>
  ),
}));

jest.mock('Components/withScrollPosition', () => ({
  __esModule: true,
  default: (Component: React.ComponentType) => Component,
}));

jest.mock('App/SelectContext', () => ({
  SelectProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="select-provider">{children}</div>
  ),
}));

jest.mock('./GameIndexFooter', () => ({
  __esModule: true,
  default: () => <div data-testid="game-index-footer" />,
}));

jest.mock('./GameIndexRefreshGameButton', () => ({
  __esModule: true,
  default: () => <button data-testid="refresh-game-button">Refresh</button>,
}));

jest.mock('./GameIndexSearchButton', () => ({
  __esModule: true,
  default: () => <button data-testid="search-button">Search</button>,
}));

jest.mock('./GameIndexSearchMenuItem', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Menus/GameIndexFilterMenu', () => ({
  __esModule: true,
  default: () => <div data-testid="filter-menu" />,
}));

jest.mock('./Menus/GameIndexSortMenu', () => ({
  __esModule: true,
  default: () => <div data-testid="sort-menu" />,
}));

jest.mock('./Menus/GameIndexViewMenu', () => ({
  __esModule: true,
  default: () => <div data-testid="view-menu" />,
}));

jest.mock('./Overview/GameIndexOverviews', () => ({
  __esModule: true,
  default: () => <div data-testid="game-index-overviews" />,
}));

jest.mock('./Overview/Options/GameIndexOverviewOptionsModal', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Posters/GameIndexPosters', () => ({
  __esModule: true,
  default: () => <div data-testid="game-index-posters" />,
}));

jest.mock('./Posters/Options/GameIndexPosterOptionsModal', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Select/GameIndexSelectAllButton', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Select/GameIndexSelectAllMenuItem', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Select/GameIndexSelectFooter', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Select/GameIndexSelectModeButton', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Select/GameIndexSelectModeMenuItem', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('./Table/GameIndexTable', () => ({
  __esModule: true,
  default: () => <div data-testid="game-index-table" />,
}));

jest.mock('./Table/GameIndexTableOptions', () => ({
  __esModule: true,
  default: () => null,
}));

jest.mock('InteractiveImport/InteractiveImportModal', () => ({
  __esModule: true,
  default: () => null,
}));

import { useSelector } from 'react-redux';
import createGameClientSideCollectionItemsSelector from 'Store/Selectors/createGameClientSideCollectionItemsSelector';

const mockedUseSelector = useSelector as jest.Mock;
const mockedCreateSelector =
  createGameClientSideCollectionItemsSelector as jest.Mock;

describe('GameIndex', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render loading indicator when fetching', () => {
    mockedCreateSelector.mockReturnValue(() => ({
      isFetching: true,
      isPopulated: false,
      error: null,
      totalItems: 0,
      items: [],
      columns: [],
      selectedFilterKey: 'all',
      filters: [],
      customFilters: [],
      sortKey: 'sortTitle',
      sortDirection: 'ascending',
      view: 'table',
    }));

    mockedUseSelector.mockImplementation((selector: () => unknown) => {
      if (typeof selector === 'function') {
        return selector();
      }
      return null;
    });

    render(<GameIndex />);

    expect(screen.getByTestId('loading-indicator')).toBeInTheDocument();
  });

  it('should render error alert when there is an error', () => {
    mockedCreateSelector.mockReturnValue(() => ({
      isFetching: false,
      isPopulated: true,
      error: { message: 'Test error' },
      totalItems: 0,
      items: [],
      columns: [],
      selectedFilterKey: 'all',
      filters: [],
      customFilters: [],
      sortKey: 'sortTitle',
      sortDirection: 'ascending',
      view: 'table',
    }));

    mockedUseSelector.mockImplementation((selector: () => unknown) => {
      if (typeof selector === 'function') {
        return selector();
      }
      return null;
    });

    render(<GameIndex />);

    expect(screen.getByTestId('alert')).toBeInTheDocument();
    expect(screen.getByText('UnableToLoadGames')).toBeInTheDocument();
  });

  it('should render NoGame component when empty', () => {
    mockedCreateSelector.mockReturnValue(() => ({
      isFetching: false,
      isPopulated: true,
      error: null,
      totalItems: 0,
      items: [],
      columns: [],
      selectedFilterKey: 'all',
      filters: [],
      customFilters: [],
      sortKey: 'sortTitle',
      sortDirection: 'ascending',
      view: 'table',
    }));

    mockedUseSelector.mockImplementation((selector: () => unknown) => {
      if (typeof selector === 'function') {
        return selector();
      }
      return null;
    });

    render(<GameIndex />);

    expect(screen.getByTestId('no-game')).toBeInTheDocument();
  });

  it('should render page toolbar', () => {
    mockedCreateSelector.mockReturnValue(() => ({
      isFetching: false,
      isPopulated: true,
      error: null,
      totalItems: 1,
      items: [{ id: 1, sortTitle: 'Test Game' }],
      columns: [],
      selectedFilterKey: 'all',
      filters: [],
      customFilters: [],
      sortKey: 'sortTitle',
      sortDirection: 'ascending',
      view: 'table',
    }));

    mockedUseSelector.mockImplementation((selector: () => unknown) => {
      if (typeof selector === 'function') {
        return selector();
      }
      return null;
    });

    render(<GameIndex />);

    expect(screen.getByTestId('page-toolbar')).toBeInTheDocument();
    expect(screen.getByTestId('refresh-game-button')).toBeInTheDocument();
    expect(screen.getByTestId('search-button')).toBeInTheDocument();
  });
});
