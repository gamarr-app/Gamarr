import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { GameFile } from 'GameFile/GameFile';
import { SortDirection } from 'Helpers/Props/sortDirections';
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
    items,
    columns,
    sortKey,
    sortDirection,
    onSortPress,
    onTableOptionChange,
    onDeletePress,
  } = props;

  return (
    <div>
      {!items.length && (
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
