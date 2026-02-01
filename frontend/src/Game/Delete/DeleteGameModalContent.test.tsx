import { fireEvent, render, screen } from '@testing-library/react';
import DeleteGameModalContent from './DeleteGameModalContent';

import '@testing-library/jest-dom';

const mockDispatch = jest.fn();

jest.mock('react-redux', () => ({
  useDispatch: () => mockDispatch,
  useSelector: jest.fn(() => ({ addImportExclusion: false })),
}));

jest.mock('Game/useGame', () => ({
  __esModule: true,
  default: () => ({
    title: 'Test Game',
    path: '/games/test-game',
    collection: { igdbId: 123 },
    statistics: {
      gameFileCount: 2,
      sizeOnDisk: 1073741824,
    },
  }),
}));

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string, tokens: Record<string, string | number> = {}) => {
    const translations: Record<string, string> = {
      DeleteHeader: `Delete - ${tokens.title}`,
      AddListExclusion: 'Add List Exclusion',
      AddListExclusionGameHelpText: 'Add exclusion help text',
      DeleteGameFiles: `Delete ${tokens.gameFileCount} Game Files`,
      DeleteGameFilesHelpText: 'Delete files help text',
      DeleteGameFolder: 'Delete Game Folder',
      DeleteGameFolderHelpText: 'Delete folder help text',
      DeleteGameFolderConfirmation: `Game folder ${tokens.path} will be deleted`,
      DeleteGameFolderGameCount: `${tokens.gameFileCount} game files (${tokens.size})`,
      Close: 'Close',
      Delete: 'Delete',
    };
    return translations[key] || key;
  },
}));

jest.mock('Utilities/Number/formatBytes', () => ({
  __esModule: true,
  default: () => '1.0 GiB',
}));

jest.mock('Components/Icon', () => ({
  __esModule: true,
  default: ({ name }: { name: string }) => (
    <span data-testid="icon" data-name={name} />
  ),
}));

jest.mock('Components/Markdown/InlineMarkdown', () => ({
  __esModule: true,
  default: ({ data }: { data: string }) => <span>{data}</span>,
}));

jest.mock('Store/Actions/gameActions', () => ({
  deleteGame: jest.fn((payload) => ({ type: 'DELETE_GAME', payload })),
  setDeleteOption: jest.fn((payload) => ({
    type: 'SET_DELETE_OPTION',
    payload,
  })),
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
    <label>{children}</label>
  ),
}));

jest.mock('Components/Form/FormInputGroup', () => ({
  __esModule: true,
  default: ({
    name,
    value,
    onChange,
  }: {
    name: string;
    value: boolean;
    onChange: (change: { name: string; value: boolean }) => void;
  }) => (
    <input
      data-testid={`check-${name}`}
      type="checkbox"
      checked={value}
      onChange={() => onChange({ name, value: !value })}
    />
  ),
}));

jest.mock('Components/Link/Button', () => ({
  __esModule: true,
  default: ({
    children,
    onPress,
  }: {
    children: React.ReactNode;
    onPress?: () => void;
  }) => <button onClick={onPress}>{children}</button>,
}));

jest.mock('Components/Modal/ModalContent', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalHeader', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalBody', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalFooter', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
}));

jest.mock('./DeleteGameModalContent.css', () => ({
  pathContainer: 'pathContainer',
  pathIcon: 'pathIcon',
  deleteFilesMessage: 'deleteFilesMessage',
  folderPath: 'folderPath',
  deleteCount: 'deleteCount',
}));

describe('DeleteGameModalContent', () => {
  const mockOnModalClose = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render the game title in header', () => {
    render(
      <DeleteGameModalContent gameId={1} onModalClose={mockOnModalClose} />
    );
    expect(screen.getByText('Delete - Test Game')).toBeInTheDocument();
  });

  it('should render the game path', () => {
    render(
      <DeleteGameModalContent gameId={1} onModalClose={mockOnModalClose} />
    );
    expect(screen.getByText('/games/test-game')).toBeInTheDocument();
  });

  it('should render delete files checkbox unchecked by default', () => {
    render(
      <DeleteGameModalContent gameId={1} onModalClose={mockOnModalClose} />
    );
    const checkbox = screen.getByTestId('check-deleteFiles');
    expect(checkbox).not.toBeChecked();
  });

  it('should show confirmation message when delete files is checked', () => {
    render(
      <DeleteGameModalContent gameId={1} onModalClose={mockOnModalClose} />
    );
    const checkbox = screen.getByTestId('check-deleteFiles');
    fireEvent.click(checkbox);
    expect(
      screen.getByText(/test-game.*will be deleted/)
    ).toBeInTheDocument();
  });

  it('should call onModalClose when close button is pressed', () => {
    render(
      <DeleteGameModalContent gameId={1} onModalClose={mockOnModalClose} />
    );
    fireEvent.click(screen.getByText('Close'));
    expect(mockOnModalClose).toHaveBeenCalled();
  });

  it('should dispatch deleteGame and close modal when delete is confirmed', () => {
    const { deleteGame } = require('Store/Actions/gameActions');
    render(
      <DeleteGameModalContent gameId={1} onModalClose={mockOnModalClose} />
    );
    fireEvent.click(screen.getByText('Delete'));
    expect(deleteGame).toHaveBeenCalledWith(
      expect.objectContaining({
        id: 1,
        deleteFiles: false,
        addImportExclusion: false,
      })
    );
    expect(mockOnModalClose).toHaveBeenCalled();
  });
});
