import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './ImportGameRootFolderRow.css';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface ImportGameRootFolderRowProps {
  id: number;
  path: string;
  freeSpace?: number;
  unmappedFolders?: UnmappedFolder[];
  onDeletePress: () => void;
}

function ImportGameRootFolderRow(props: ImportGameRootFolderRowProps) {
  const {
    id,
    path,
    freeSpace = 0,
    unmappedFolders = [],
    onDeletePress,
  } = props;

  const unmappedFoldersCount = unmappedFolders.length || '-';

  return (
    <TableRow>
      <TableRowCell>
        <Link className={styles.link} to={`/add/import/${id}`}>
          {path}
        </Link>
      </TableRowCell>

      <TableRowCell className={styles.freeSpace}>
        {formatBytes(freeSpace) || '-'}
      </TableRowCell>

      <TableRowCell className={styles.unmappedFolders}>
        {unmappedFoldersCount}
      </TableRowCell>

      <TableRowCell className={styles.actions}>
        <IconButton
          title={translate('RemoveRootFolder')}
          name={icons.REMOVE}
          onPress={onDeletePress}
        />
      </TableRowCell>
    </TableRow>
  );
}

export default ImportGameRootFolderRow;
