import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { deleteRootFolder } from 'Store/Actions/rootFolderActions';
import ImportGameRootFolderRow from './ImportGameRootFolderRow';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface ImportGameRootFolderRowConnectorProps {
  id: number;
  path: string;
  freeSpace: number;
  unmappedFolders: UnmappedFolder[];
}

function ImportGameRootFolderRowConnector(
  props: ImportGameRootFolderRowConnectorProps
) {
  const { id, path, freeSpace, unmappedFolders } = props;
  const dispatch = useDispatch();

  const onDeletePress = useCallback(() => {
    dispatch(deleteRootFolder({ id }));
  }, [dispatch, id]);

  return (
    <ImportGameRootFolderRow
      id={id}
      path={path}
      freeSpace={freeSpace}
      unmappedFolders={unmappedFolders}
      onDeletePress={onDeletePress}
    />
  );
}

export default ImportGameRootFolderRowConnector;
