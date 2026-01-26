import { ComponentType } from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createCollectionSelector from 'Store/Selectors/createCollectionSelector';

interface CollectionItemConnectorProps {
  collectionId: number;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- Component receives merged props from Redux and parent
  component: ComponentType<any>;
  [key: string]: unknown;
}

function createCollectionItemSelector(collectionId: number) {
  return createSelector(
    (state: AppState) => createCollectionSelector()(state, { collectionId }),
    (collection) => {
      // If a game is deleted this selector may fire before the parent
      // selectors, which will result in an undefined game, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show a game that has no information available.

      if (!collection) {
        return { id: undefined };
      }

      const allGenres = collection.games.flatMap((game) => game.genres);

      return {
        ...collection,
        games: [...collection.games].sort((a, b) => b.year - a.year),
        genres: Array.from(new Set(allGenres)),
      };
    }
  );
}

function CollectionItemConnector(props: CollectionItemConnectorProps) {
  const { collectionId, component: ItemComponent, ...otherProps } = props;

  const collectionData = useSelector(
    createCollectionItemSelector(collectionId)
  );

  if (!collectionData.id) {
    return null;
  }

  return <ItemComponent {...otherProps} {...collectionData} />;
}

export default CollectionItemConnector;
