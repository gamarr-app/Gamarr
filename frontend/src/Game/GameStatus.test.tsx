import { render, screen } from '@testing-library/react';
import { ReactNode } from 'react';
import GameStatus from './GameStatus';

import '@testing-library/jest-dom';

// Mock all the hooks and selectors
jest.mock('react-redux', () => ({
  useSelector: jest.fn(),
}));

jest.mock('Game/useGame', () => ({
  __esModule: true,
  default: jest.fn(),
}));

jest.mock('GameFile/useGameFile', () => ({
  __esModule: true,
  default: jest.fn(),
}));

jest.mock('Store/Selectors/createQueueItemSelector', () => ({
  createQueueItemSelectorForHook: jest.fn(() => () => null),
}));

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

jest.mock('./GameQuality', () => ({
  __esModule: true,
  default: ({ title }: { title: string }) => (
    <span data-testid="game-quality">{title}</span>
  ),
}));

jest.mock('Components/Icon', () => ({
  __esModule: true,
  default: ({ name, title }: { name: string; title: string }) => (
    <span data-testid="icon" data-name={name} data-title={title}>
      {title}
    </span>
  ),
}));

jest.mock('Components/ProgressBar', () => ({
  __esModule: true,
  default: ({ title }: { title: string }) => (
    <div data-testid="progress-bar">{title}</div>
  ),
}));

jest.mock('Activity/Queue/QueueDetails', () => ({
  __esModule: true,
  default: ({ progressBar }: { progressBar: ReactNode }) => (
    <div data-testid="queue-details">{progressBar}</div>
  ),
}));

import { useSelector } from 'react-redux';
import useGame from 'Game/useGame';
import useGameFile from 'GameFile/useGameFile';

const mockedUseGame = useGame as jest.Mock;
const mockedUseGameFile = useGameFile as jest.Mock;
const mockedUseSelector = useSelector as jest.Mock;

describe('GameStatus', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockedUseSelector.mockReturnValue(null); // no queue item
  });

  it('should render nothing when the game is no longer in the store', () => {
    mockedUseGame.mockReturnValue(undefined);
    mockedUseGameFile.mockReturnValue(undefined);

    const { container } = render(
      <GameStatus gameId={1} gameFileId={undefined} />
    );

    expect(container.innerHTML).toBe('');
  });

  it('should render queue progress when an item is queued', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue(undefined);
    mockedUseSelector.mockReturnValue({
      gameId: 1,
      size: 1000,
      sizeLeft: 500,
      status: 'downloading',
      trackedDownloadStatus: 'ok',
      trackedDownloadState: 'downloading',
    });

    render(<GameStatus gameId={1} gameFileId={undefined} />);

    expect(screen.getByTestId('queue-details')).toBeInTheDocument();
    expect(screen.getByTestId('progress-bar')).toHaveTextContent('50.0%');
  });

  it('should prefer queue progress over an existing file', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: true,
    });
    mockedUseGameFile.mockReturnValue({
      quality: { quality: { name: 'Repack' } },
      qualityCutoffNotMet: false,
      size: 1000000,
    });
    mockedUseSelector.mockReturnValue({
      gameId: 1,
      size: 1000,
      sizeLeft: 250,
      status: 'downloading',
      trackedDownloadStatus: 'ok',
      trackedDownloadState: 'downloading',
    });

    render(<GameStatus gameId={1} gameFileId={1} />);

    expect(screen.getByTestId('queue-details')).toBeInTheDocument();
    expect(screen.queryByTestId('game-quality')).not.toBeInTheDocument();
  });

  it('should render the downloading icon when the game is grabbed', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: true,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    render(<GameStatus gameId={1} gameFileId={undefined} />);

    expect(screen.getByTestId('icon')).toHaveAttribute(
      'data-title',
      'GameIsDownloading'
    );
  });

  it('should render the quality badge when the game has a file', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue({
      quality: { quality: { name: 'Repack' } },
      qualityCutoffNotMet: false,
      size: 1000000,
    });

    render(<GameStatus gameId={1} gameFileId={1} />);

    expect(screen.getByTestId('game-quality')).toHaveTextContent(
      'GameDownloaded'
    );
  });

  it('should render the unmonitored icon when the game is not monitored', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: false,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    render(<GameStatus gameId={1} gameFileId={undefined} />);

    expect(screen.getByTestId('icon')).toHaveAttribute(
      'data-title',
      'GameIsNotMonitored'
    );
  });

  it('should render the missing icon when an available game has no file', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    render(<GameStatus gameId={1} gameFileId={undefined} />);

    expect(screen.getByTestId('icon')).toHaveAttribute(
      'data-title',
      'GameMissingFromDisk'
    );
  });

  it('should render the not-available icon when the game is unreleased', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: false,
      monitored: true,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    render(<GameStatus gameId={1} gameFileId={undefined} />);

    expect(screen.getByTestId('icon')).toHaveAttribute(
      'data-title',
      'GameIsNotAvailable'
    );
  });
});
