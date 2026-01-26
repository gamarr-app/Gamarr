import _ from 'lodash';
import { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useHistory } from 'react-router-dom';
import { createSelector } from 'reselect';
import {
  addRootFolder,
  deleteRootFolder,
  fetchRootFolders,
} from 'Store/Actions/rootFolderActions';
import createRootFoldersSelector from 'Store/Selectors/createRootFoldersSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import ImportGameSelectFolder from './ImportGameSelectFolder';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface RootFolderItemFromState {
  id: number;
  path: string;
  freeSpace?: number;
  unmappedFolders?: object[];
}

interface RootFoldersState {
  isSaving: boolean;
  saveError?: object;
  items: RootFolderItemFromState[];
  isFetching?: boolean;
  isPopulated?: boolean;
}

const createMapStateToProps = createSelector(
  createRootFoldersSelector(),
  createSystemStatusSelector(),
  (rootFolders: RootFoldersState, systemStatus) => {
    return {
      ...rootFolders,
      isWindows: systemStatus.isWindows,
    };
  }
);

function ImportGameSelectFolderConnector() {
  const dispatch = useDispatch();
  const history = useHistory();
  const state = useSelector(createMapStateToProps);
  const prevItemsRef = useRef<RootFolderItemFromState[]>(state.items);
  const prevIsSavingRef = useRef(state.isSaving);

  const { items, isSaving, saveError, isWindows, isFetching, isPopulated } =
    state;

  useEffect(() => {
    dispatch(fetchRootFolders());
  }, [dispatch]);

  useEffect(() => {
    if (prevIsSavingRef.current && !isSaving && !saveError) {
      const newRootFolders = _.differenceBy(
        items,
        prevItemsRef.current,
        (item) => item.id
      );

      if (newRootFolders.length === 1) {
        history.push(`/add/import/${newRootFolders[0].id}`);
      }
    }

    prevItemsRef.current = items;
    prevIsSavingRef.current = isSaving;
  }, [items, isSaving, saveError, history]);

  const onNewRootFolderSelect = useCallback(
    (path: string) => {
      dispatch(addRootFolder({ path }));
    },
    [dispatch]
  );

  const onDeleteRootFolderPress = useCallback(
    (id: number) => {
      dispatch(deleteRootFolder({ id }));
    },
    [dispatch]
  );

  return (
    <ImportGameSelectFolder
      isSaving={isSaving}
      isWindows={isWindows || false}
      isFetching={isFetching || false}
      isPopulated={isPopulated || false}
      items={items.map((item) => ({
        ...item,
        freeSpace: item.freeSpace || 0,
        unmappedFolders: (item.unmappedFolders || []) as UnmappedFolder[],
      }))}
      onNewRootFolderSelect={onNewRootFolderSelect}
      onDeleteRootFolderPress={onDeleteRootFolderPress}
    />
  );
}

export default ImportGameSelectFolderConnector;
