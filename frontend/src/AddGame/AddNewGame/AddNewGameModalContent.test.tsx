import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';
import AddNewGameModalContent from './AddNewGameModalContent';

import '@testing-library/jest-dom';

// Add String.prototype.contains polyfill (matches frontend/src/polyfills.ts)
declare global {
  interface String {
    contains(str: string, startIndex?: number): boolean;
  }
}

String.prototype.contains = function (
  this: string,
  str: string,
  startIndex?: number
) {
  return this.indexOf(str, startIndex) !== -1;
};

// Mock translate
jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

// Mock components
jest.mock('Components/Modal/ModalContent', () => ({
  __esModule: true,
  default: ({
    children,
    onModalClose,
  }: {
    children: React.ReactNode;
    onModalClose: () => void;
  }) => (
    <div data-testid="modal-content" onClick={onModalClose}>
      {children}
    </div>
  ),
}));

jest.mock('Components/Modal/ModalHeader', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="modal-header">{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalBody', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="modal-body">{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalFooter', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="modal-footer">{children}</div>
  ),
}));

jest.mock('Components/Form/Form', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <form data-testid="form">{children}</form>
  ),
}));

jest.mock('Components/Form/FormGroup', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="form-group">{children}</div>
  ),
}));

jest.mock('Components/Form/FormLabel', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <label data-testid="form-label">{children}</label>
  ),
}));

jest.mock('Components/Form/FormInputGroup', () => ({
  __esModule: true,
  default: ({
    name,
    type,
    onChange,
  }: {
    name: string;
    type: string;
    onChange: (change: { name: string; value: unknown }) => void;
  }) => (
    <input
      data-testid={`form-input-${name}`}
      data-type={type}
      onChange={(e) => onChange({ name, value: e.target.value })}
    />
  ),
}));

jest.mock('Components/Form/CheckInput', () => ({
  __esModule: true,
  default: ({
    name,
    onChange,
    value,
  }: {
    name: string;
    onChange: (change: { name: string; value: boolean }) => void;
    value: boolean;
  }) => (
    <input
      type="checkbox"
      data-testid={`check-input-${name}`}
      checked={value}
      onChange={(e) => onChange({ name, value: e.target.checked })}
    />
  ),
}));

jest.mock('Components/Link/SpinnerButton', () => ({
  __esModule: true,
  default: ({
    children,
    isSpinning,
    onPress,
  }: {
    children: React.ReactNode;
    isSpinning: boolean;
    onPress: () => void;
  }) => (
    <button
      data-testid="spinner-button"
      disabled={isSpinning}
      onClick={onPress}
    >
      {children}
    </button>
  ),
}));

jest.mock('Components/Icon', () => ({
  __esModule: true,
  default: ({ name }: { name: string }) => (
    <span data-testid="icon" data-name={name} />
  ),
}));

jest.mock('Components/Tooltip/Popover', () => ({
  __esModule: true,
  default: ({ anchor }: { anchor: React.ReactNode }) => (
    <div data-testid="popover">{anchor}</div>
  ),
}));

jest.mock('Game/GamePoster', () => ({
  __esModule: true,
  default: () => <div data-testid="game-poster" />,
}));

jest.mock('AddGame/GameMinimumAvailabilityPopoverContent', () => ({
  __esModule: true,
  default: () => <div data-testid="availability-popover-content" />,
}));

describe('AddNewGameModalContent', () => {
  const defaultProps = {
    title: 'Test Game',
    year: 2023,
    overview: 'A test game description',
    images: [],
    isAdding: false,
    addError: undefined,
    rootFolderPath: { value: '/games' },
    monitor: { value: 'gameOnly' },
    qualityProfileId: { value: 1 },
    minimumAvailability: { value: 'released' },
    searchForGame: { value: true },
    folder: 'test-game',
    tags: { value: [] },
    isSmallScreen: false,
    isWindows: false,
    onModalClose: jest.fn(),
    onInputChange: jest.fn(),
    onAddGamePress: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render the modal with game title', () => {
    render(<AddNewGameModalContent {...defaultProps} />);

    expect(screen.getByTestId('modal-header')).toHaveTextContent('Test Game');
  });

  it('should render year in header when not included in title', () => {
    render(<AddNewGameModalContent {...defaultProps} />);

    expect(screen.getByTestId('modal-header')).toHaveTextContent('(2023)');
  });

  it('should not render year when title contains the year', () => {
    render(
      <AddNewGameModalContent {...defaultProps} title="Test Game (2023)" />
    );

    const header = screen.getByTestId('modal-header');
    expect(header.textContent).toBe('Test Game (2023)');
  });

  it('should render form inputs', () => {
    render(<AddNewGameModalContent {...defaultProps} />);

    expect(screen.getByTestId('form-input-rootFolderPath')).toBeInTheDocument();
    expect(screen.getByTestId('form-input-monitor')).toBeInTheDocument();
    expect(
      screen.getByTestId('form-input-minimumAvailability')
    ).toBeInTheDocument();
    expect(
      screen.getByTestId('form-input-qualityProfileId')
    ).toBeInTheDocument();
    expect(screen.getByTestId('form-input-tags')).toBeInTheDocument();
  });

  it('should render search checkbox', () => {
    render(<AddNewGameModalContent {...defaultProps} />);

    expect(screen.getByTestId('check-input-searchForGame')).toBeInTheDocument();
  });

  it('should render add button', () => {
    render(<AddNewGameModalContent {...defaultProps} />);

    const addButton = screen.getByTestId('spinner-button');
    expect(addButton).toBeInTheDocument();
    expect(addButton).toHaveTextContent('AddGame');
  });

  it('should call onAddGamePress when add button is clicked', () => {
    const onAddGamePress = jest.fn();
    render(
      <AddNewGameModalContent
        {...defaultProps}
        onAddGamePress={onAddGamePress}
      />
    );

    fireEvent.click(screen.getByTestId('spinner-button'));

    expect(onAddGamePress).toHaveBeenCalled();
  });

  it('should call onInputChange when input changes', () => {
    const onInputChange = jest.fn();
    render(
      <AddNewGameModalContent {...defaultProps} onInputChange={onInputChange} />
    );

    fireEvent.change(screen.getByTestId('form-input-monitor'), {
      target: { value: 'none' },
    });

    expect(onInputChange).toHaveBeenCalledWith({
      name: 'monitor',
      value: 'none',
    });
  });

  it('should render game poster on large screens', () => {
    render(<AddNewGameModalContent {...defaultProps} isSmallScreen={false} />);

    expect(screen.getByTestId('game-poster')).toBeInTheDocument();
  });

  it('should not render game poster on small screens', () => {
    render(<AddNewGameModalContent {...defaultProps} isSmallScreen={true} />);

    expect(screen.queryByTestId('game-poster')).not.toBeInTheDocument();
  });

  it('should render overview when provided', () => {
    render(<AddNewGameModalContent {...defaultProps} />);

    expect(screen.getByText('A test game description')).toBeInTheDocument();
  });

  it('should disable add button when isAdding is true', () => {
    render(<AddNewGameModalContent {...defaultProps} isAdding={true} />);

    expect(screen.getByTestId('spinner-button')).toBeDisabled();
  });
});
