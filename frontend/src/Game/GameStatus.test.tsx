import { render } from '@testing-library/react';
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

  it('should not render quality badge when showMissingStatus is false and game has file', () => {
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

    const { container } = render(
      <GameStatus gameId={1} gameFileId={1} showMissingStatus={false} />
    );

    expect(container.innerHTML).toBe('');
  });

  it('should render quality badge when showMissingStatus is true and game has file', () => {
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

    const { container } = render(
      <GameStatus gameId={1} gameFileId={1} showMissingStatus={true} />
    );

    expect(container.innerHTML).not.toBe('');
  });

  it('should not render grabbed icon when showMissingStatus is false', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: true,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    const { container } = render(
      <GameStatus gameId={1} gameFileId={undefined} showMissingStatus={false} />
    );

    expect(container.innerHTML).toBe('');
  });

  it('should render grabbed icon when showMissingStatus is true', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: true,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    const { container } = render(
      <GameStatus gameId={1} gameFileId={undefined} showMissingStatus={true} />
    );

    expect(container.innerHTML).not.toBe('');
  });

  it('should render missing icon when game is available and monitored with no file', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: true,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    const { container } = render(
      <GameStatus gameId={1} gameFileId={undefined} showMissingStatus={true} />
    );

    expect(container.innerHTML).not.toBe('');
  });

  it('should render unmonitored icon when game is not monitored', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: false,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    const { container } = render(
      <GameStatus gameId={1} gameFileId={undefined} showMissingStatus={true} />
    );

    expect(container.innerHTML).not.toBe('');
  });

  it('should return null for unmonitored when showMissingStatus is false', () => {
    mockedUseGame.mockReturnValue({
      isAvailable: true,
      monitored: false,
      grabbed: false,
    });
    mockedUseGameFile.mockReturnValue(undefined);

    const { container } = render(
      <GameStatus gameId={1} gameFileId={undefined} showMissingStatus={false} />
    );

    expect(container.innerHTML).toBe('');
  });

  it('should always render queue progress when item is queued regardless of showMissingStatus', () => {
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

    const { container } = render(
      <GameStatus gameId={1} gameFileId={undefined} showMissingStatus={false} />
    );

    // Queue progress should always show even with showMissingStatus=false
    expect(container.innerHTML).not.toBe('');
  });
});
