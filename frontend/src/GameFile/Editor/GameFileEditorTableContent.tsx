import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import ProgressBar from 'Components/ProgressBar';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { GameFile } from 'GameFile/GameFile';
import { kinds } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import { createQueueItemSelectorForHook } from 'Store/Selectors/createQueueItemSelector';
import { TableOptionsChangePayload } from 'typings/Table';
import translate from 'Utilities/String/translate';
import GameFileEditorRow from './GameFileEditorRow';
import styles from './GameFileEditorTableContent.css';

interface GameFileEditorTableContentProps {
  gameId?: number;
  isDeleting: boolean;
  items: GameFile[];
  columns: Column[];
  sortKey: string;
  sortDirection: SortDirection;
  onTableOptionChange: (payload: TableOptionsChangePayload) => void;
  onSortPress: (name: string, sortDirection?: SortDirection) => void;
  onDeletePress: (id: number) => void;
}

function GameFileEditorTableContent(props: GameFileEditorTableContentProps) {
  const {
    gameId,
    items,
    columns,
    sortKey,
    sortDirection,
    onSortPress,
    onTableOptionChange,
    onDeletePress,
  } = props;

  const queueItem = useSelector(
    useMemo(() => createQueueItemSelectorForHook(gameId ?? 0), [gameId])
  );

  // A grab in flight is the usual reason this table is empty, so say so here
  // rather than floating a bare icon above it.
  const progress = queueItem?.size
    ? 100 - (queueItem.sizeLeft / queueItem.size) * 100
    : 0;

  return (
    <div>
      {queueItem ? (
        <div className={styles.downloading}>
          <div className={styles.downloadingTitle}>
            {items.length
              ? translate('DownloadingRelease', { title: queueItem.title })
              : translate('NoGameFilesDownloading', { title: queueItem.title })}
          </div>

          <ProgressBar
            progress={progress}
            kind={kinds.PURPLE}
            showText={true}
            width={150}
            title={queueItem.title}
          />
        </div>
      ) : null}

      {!items.length && !queueItem && (
        <div className={styles.blankpad}>
          {translate('NoGameFilesToManage')}
        </div>
      )}

      {!!items.length && (
        <Table
          columns={columns}
          sortKey={sortKey}
          sortDirection={sortDirection}
          onSortPress={onSortPress}
          onTableOptionChange={onTableOptionChange}
        >
          <TableBody>
            {items.map((item) => {
              return (
                <GameFileEditorRow
                  key={item.id}
                  columns={columns}
                  {...item}
                  onDeletePress={onDeletePress}
                />
              );
            })}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

export default GameFileEditorTableContent;
