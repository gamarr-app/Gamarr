import _ from 'lodash';
import { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useParams } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { ImportGameItem } from 'App/State/ImportGameAppState';
import { setAddGameDefault } from 'Store/Actions/addGameActions';
import {
  clearImportGame,
  importGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import ImportGame from './ImportGame';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface RootFolderItem {
  id: number;
  path: string;
  unmappedFolders?: UnmappedFolder[];
  [key: string]: unknown;
}

function createImportGameSelector(rootFolderId: number) {
  return createSelector(
    (state: AppState) => state.rootFolders,
    (state: AppState) => state.addGame,
    (state: AppState) => state.importGame,
    (state: AppState) => state.settings.qualityProfiles,
    (rootFolders, addGame, importGameState, qualityProfiles) => {
      const {
        isFetching: rootFoldersFetching,
        isPopulated: rootFoldersPopulated,
        error: rootFoldersError,
        items,
      } = rootFolders;

      const result = {
        rootFolderId,
        rootFoldersFetching,
        rootFoldersPopulated,
        rootFoldersError,
        qualityProfiles: qualityProfiles.items,
        defaultQualityProfileId: addGame.defaults.qualityProfileId,
      };

      if (items.length) {
        const rootFolder = _.find(items, { id: rootFolderId }) as
          | RootFolderItem
          | undefined;

        return {
          ...result,
          ...rootFolder,
          items: importGameState.items,
        };
      }

      return {
        ...result,
        items: [] as ImportGameItem[],
      };
    }
  );
}

function ImportGameConnector() {
  const { rootFolderId: rootFolderIdParam } = useParams<{
    rootFolderId: string;
  }>();
  const rootFolderId = parseInt(rootFolderIdParam || '0');

  const dispatch = useDispatch();

  const selector = createImportGameSelector(rootFolderId);
  const {
    rootFoldersFetching,
    rootFoldersPopulated,
    rootFoldersError,
    qualityProfiles,
    defaultQualityProfileId,
    items,
    ...otherProps
  } = useSelector(selector);

  // Mount effect
  useEffect(() => {
    dispatch(fetchRootFolders({ id: rootFolderId, timeout: false }));

    let setDefaults = false;
    const setDefaultPayload: Record<string, unknown> = {};

    if (
      !defaultQualityProfileId ||
      !qualityProfiles.some((p) => p.id === defaultQualityProfileId)
    ) {
      setDefaults = true;
      setDefaultPayload.qualityProfileId = qualityProfiles[0]?.id;
    }

    if (setDefaults) {
      dispatch(setAddGameDefault(setDefaultPayload));
    }

    return () => {
      dispatch(clearImportGame());
    };
    // Only run on mount/unmount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onInputChange = useCallback(
    (ids: string[], name: string, value: unknown) => {
      dispatch(setAddGameDefault({ [name]: value }));

      ids.forEach((id) => {
        dispatch(
          setImportGameValue({
            id,
            [name]: value,
          })
        );
      });
    },
    [dispatch]
  );

  const onImportPress = useCallback(
    (ids: string[]) => {
      dispatch(importGame({ ids }));
    },
    [dispatch]
  );

  return (
    <ImportGame
      {...otherProps}
      rootFolderId={rootFolderId}
      rootFoldersFetching={rootFoldersFetching}
      rootFoldersPopulated={rootFoldersPopulated}
      rootFoldersError={rootFoldersError}
      items={items}
      onInputChange={onInputChange}
      onImportPress={onImportPress}
    />
  );
}

export default ImportGameConnector;
