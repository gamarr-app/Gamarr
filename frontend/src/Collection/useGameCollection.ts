import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export function createGameCollectionSelector(collectionId?: number) {
  return createSelector(
    (state: AppState) => state.gameCollections.itemMap,
    (state: AppState) => state.gameCollections.items,
    (itemMap, allGameCollections) => {
      if (!collectionId) {
        return undefined;
      }

      const index = itemMap[collectionId];

      if (index === undefined) {
        return undefined;
      }

      return allGameCollections[index];
    }
  );
}

function useGameCollection(collectionId: number | undefined) {
  return useSelector(
    useMemo(() => createGameCollectionSelector(collectionId), [collectionId])
  );
}

export default useGameCollection;
