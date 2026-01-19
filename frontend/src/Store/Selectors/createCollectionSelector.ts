import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export function createCollectionSelectorForHook(igdbId: number) {
  return createSelector(
    (state: AppState) => state.gameCollections.items,
    (collections) => {
      return collections.find((item) => item.igdbId === igdbId);
    }
  );
}

function createCollectionSelector() {
  return createSelector(
    (_: AppState, { collectionId }: { collectionId: number }) => collectionId,
    (state: AppState) => state.gameCollections.itemMap,
    (state: AppState) => state.gameCollections.items,
    (collectionId, itemMap, allCollections) => {
      return allCollections && itemMap && collectionId in itemMap
        ? allCollections[itemMap[collectionId]]
        : undefined;
    }
  );
}

export default createCollectionSelector;
