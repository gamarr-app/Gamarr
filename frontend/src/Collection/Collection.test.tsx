import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';
import Collection from './Collection';

import '@testing-library/jest-dom';

// Mock translate
jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

// Mock lodash
jest.mock('lodash', () => ({
  reduce: jest.requireActual('lodash').reduce,
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

jest.mock('Components/Page/PageContent', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="page-content">{children}</div>
  ),
}));

jest.mock('Components/Page/PageContentBody', () => {
  const { forwardRef } = jest.requireActual('react');
  return {
    __esModule: true,
    default: forwardRef(
      (
        { children }: { children: React.ReactNode },
        ref: React.Ref<HTMLDivElement>
      ) => (
        <div ref={ref} data-testid="page-content-body">
          {children}
        </div>
      )
    ),
  };
});

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
  default: ({
    isDisabled,
    label,
    onPress,
  }: {
    isDisabled?: boolean;
    label: string;
    onPress?: () => void;
  }) => (
    <button
      data-testid={`toolbar-button-${label}`}
      disabled={isDisabled}
      onClick={onPress}
    >
      {label}
    </button>
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

jest.mock('./CollectionFooter', () => ({
  __esModule: true,
  default: ({
    selectedIds,
    onUpdateSelectedPress,
  }: {
    selectedIds: number[];
    onUpdateSelectedPress: (payload: object) => void;
  }) => (
    <div data-testid="collection-footer">
      <span data-testid="selected-count">{selectedIds.length}</span>
      <button
        data-testid="update-selected"
        onClick={() => onUpdateSelectedPress({ test: true })}
      >
        Update
      </button>
    </div>
  ),
}));

jest.mock('./Menus/GameCollectionFilterMenu', () => ({
  __esModule: true,
  default: () => <div data-testid="filter-menu" />,
}));

jest.mock('./Menus/GameCollectionSortMenu', () => ({
  __esModule: true,
  default: () => <div data-testid="sort-menu" />,
}));

jest.mock('./NoGameCollections', () => ({
  __esModule: true,
  default: () => <div data-testid="no-collections">No collections found</div>,
}));

jest.mock('./Overview/CollectionOverviewsConnector', () => ({
  __esModule: true,
  default: ({
    selectedState,
    onSelectedChange,
  }: {
    selectedState: Record<number, boolean>;
    onSelectedChange: (props: {
      id: number;
      value: boolean;
      shiftKey?: boolean;
    }) => void;
  }) => (
    <div data-testid="collection-overviews">
      <span data-testid="selection-state">{JSON.stringify(selectedState)}</span>
      <button
        data-testid="select-item"
        onClick={() => onSelectedChange({ id: 1, value: true })}
      >
        Select Item
      </button>
    </div>
  ),
}));

jest.mock('./Overview/Options/CollectionOverviewOptionsModal', () => ({
  __esModule: true,
  default: () => null,
}));

describe('Collection', () => {
  const defaultProps = {
    isFetching: false,
    isPopulated: true,
    isSaving: false,
    isAdding: false,
    error: undefined,
    saveError: undefined,
    totalItems: 0,
    items: [],
    selectedFilterKey: 'all',
    filters: [],
    customFilters: [],
    sortKey: 'sortTitle',
    sortDirection: 'ascending' as const,
    view: 'overview',
    isRefreshingCollections: false,
    isSmallScreen: false,
    onSortSelect: jest.fn(),
    onFilterSelect: jest.fn(),
    onScroll: jest.fn(),
    onUpdateSelectedPress: jest.fn(),
    onRefreshGameCollectionsPress: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render loading indicator when fetching', () => {
    render(
      <Collection {...defaultProps} isFetching={true} isPopulated={false} />
    );

    expect(screen.getByTestId('loading-indicator')).toBeInTheDocument();
  });

  it('should render error alert when there is an error', () => {
    render(
      <Collection
        {...defaultProps}
        error={{ message: 'Test error' }}
        isPopulated={true}
      />
    );

    expect(screen.getByTestId('alert')).toBeInTheDocument();
    expect(screen.getByText('UnableToLoadCollections')).toBeInTheDocument();
  });

  it('should render NoGameCollections when empty', () => {
    render(
      <Collection
        {...defaultProps}
        isPopulated={true}
        totalItems={0}
        items={[]}
      />
    );

    expect(screen.getByTestId('no-collections')).toBeInTheDocument();
  });

  it('should render page toolbar with refresh button', () => {
    render(<Collection {...defaultProps} />);

    expect(screen.getByTestId('page-toolbar')).toBeInTheDocument();
    expect(
      screen.getByTestId('toolbar-button-RefreshCollections')
    ).toBeInTheDocument();
  });

  it('should render select all button', () => {
    render(<Collection {...defaultProps} />);

    expect(screen.getByTestId('toolbar-button-SelectAll')).toBeInTheDocument();
  });

  it('should call onRefreshGameCollectionsPress when refresh button is clicked', () => {
    const onRefreshGameCollectionsPress = jest.fn();
    render(
      <Collection
        {...defaultProps}
        totalItems={1}
        items={[{ id: 1, sortTitle: 'Test' }]}
        onRefreshGameCollectionsPress={onRefreshGameCollectionsPress}
      />
    );

    fireEvent.click(screen.getByTestId('toolbar-button-RefreshCollections'));

    expect(onRefreshGameCollectionsPress).toHaveBeenCalled();
  });

  it('should disable buttons when no collections exist', () => {
    render(<Collection {...defaultProps} totalItems={0} items={[]} />);

    expect(
      screen.getByTestId('toolbar-button-RefreshCollections')
    ).toBeDisabled();
    expect(screen.getByTestId('toolbar-button-SelectAll')).toBeDisabled();
  });

  it('should render collection overviews when loaded with items', () => {
    render(
      <Collection
        {...defaultProps}
        totalItems={1}
        items={[{ id: 1, sortTitle: 'Test Collection' }]}
      />
    );

    expect(screen.getByTestId('collection-overviews')).toBeInTheDocument();
  });

  it('should render collection footer when loaded', () => {
    render(
      <Collection
        {...defaultProps}
        totalItems={1}
        items={[{ id: 1, sortTitle: 'Test Collection' }]}
      />
    );

    expect(screen.getByTestId('collection-footer')).toBeInTheDocument();
  });

  it('should toggle select all when select all button is clicked', () => {
    render(
      <Collection
        {...defaultProps}
        totalItems={1}
        items={[{ id: 1, sortTitle: 'Test Collection' }]}
      />
    );

    const selectAllButton = screen.getByTestId('toolbar-button-SelectAll');
    fireEvent.click(selectAllButton);

    // After clicking, the button label should change to UnselectAll
    expect(
      screen.getByTestId('toolbar-button-UnselectAll')
    ).toBeInTheDocument();
  });

  it('should handle item selection', () => {
    render(
      <Collection
        {...defaultProps}
        totalItems={1}
        items={[{ id: 1, sortTitle: 'Test Collection' }]}
      />
    );

    fireEvent.click(screen.getByTestId('select-item'));

    // Check that the selected count in footer is updated
    expect(screen.getByTestId('selected-count')).toHaveTextContent('1');
  });

  it('should call onUpdateSelectedPress with selected ids', () => {
    const onUpdateSelectedPress = jest.fn();
    render(
      <Collection
        {...defaultProps}
        totalItems={1}
        items={[{ id: 1, sortTitle: 'Test Collection' }]}
        onUpdateSelectedPress={onUpdateSelectedPress}
      />
    );

    // First select an item
    fireEvent.click(screen.getByTestId('select-item'));

    // Then click update
    fireEvent.click(screen.getByTestId('update-selected'));

    expect(onUpdateSelectedPress).toHaveBeenCalledWith({
      collectionIds: [1],
      test: true,
    });
  });
});
