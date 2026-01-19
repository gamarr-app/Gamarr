import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export function createGameCollectionSelector(collectionId?: number) {
  return createSelector(
    (state: AppState) => state.gameCollections.itemMap,
    (state: AppState) => state.gameCollections.items,
    (itemMap, allGameCollections) => {
      return collectionId
        ? allGameCollections[itemMap[collectionId]]
        : undefined;
    }
  );
}

function useGameCollection(collectionId: number | undefined) {
  return useSelector(createGameCollectionSelector(collectionId));
}

export default useGameCollection;
