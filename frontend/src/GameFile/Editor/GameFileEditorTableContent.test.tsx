import { render, screen } from '@testing-library/react';
import { ReactNode } from 'react';
import { GameFile } from 'GameFile/GameFile';
import GameFileEditorTableContent from './GameFileEditorTableContent';

import '@testing-library/jest-dom';

jest.mock('react-redux', () => ({
  useSelector: jest.fn(),
}));

jest.mock('Store/Selectors/createQueueItemSelector', () => ({
  createQueueItemSelectorForHook: jest.fn(() => () => null),
}));

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

jest.mock('Components/ProgressBar', () => ({
  __esModule: true,
  default: ({ progress }: { progress: number }) => (
    <div data-testid="progress-bar">{progress.toFixed(1)}</div>
  ),
}));

jest.mock('Components/Table/Table', () => ({
  __esModule: true,
  default: ({ children }: { children: ReactNode }) => (
    <div data-testid="table">{children}</div>
  ),
}));

jest.mock('Components/Table/TableBody', () => ({
  __esModule: true,
  default: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));

jest.mock('./GameFileEditorRow', () => ({
  __esModule: true,
  default: () => <div data-testid="file-row" />,
}));

import { useSelector } from 'react-redux';

const mockedUseSelector = useSelector as jest.Mock;

const gameFile = { id: 1, gameId: 1 } as GameFile;

function renderContent(items: GameFile[]) {
  return render(
    <GameFileEditorTableContent
      gameId={1}
      isDeleting={false}
      items={items}
      columns={[]}
      sortKey="relativePath"
      sortDirection="ascending"
      onTableOptionChange={jest.fn()}
      onSortPress={jest.fn()}
      onDeletePress={jest.fn()}
    />
  );
}

describe('GameFileEditorTableContent', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockedUseSelector.mockReturnValue(null);
  });

  it('should show the empty message when there are no files and nothing queued', () => {
    renderContent([]);

    expect(screen.getByText('NoGameFilesToManage')).toBeInTheDocument();
    expect(screen.queryByTestId('progress-bar')).not.toBeInTheDocument();
  });

  it('should explain the empty table with the download in progress', () => {
    mockedUseSelector.mockReturnValue({
      title: 'Some.Game-GROUP',
      size: 100,
      sizeLeft: 97,
    });

    renderContent([]);

    expect(screen.getByText('NoGameFilesDownloading')).toBeInTheDocument();
    expect(screen.getByTestId('progress-bar')).toHaveTextContent('3.0');
    expect(screen.queryByText('NoGameFilesToManage')).not.toBeInTheDocument();
  });

  it('should still show a download in progress above an existing file', () => {
    mockedUseSelector.mockReturnValue({
      title: 'Some.Game-GROUP',
      size: 100,
      sizeLeft: 50,
    });

    renderContent([gameFile]);

    expect(screen.getByText('DownloadingRelease')).toBeInTheDocument();
    expect(screen.getByTestId('table')).toBeInTheDocument();
  });
});
