import { Component, ComponentType } from 'react';
import { connect, ConnectedProps } from 'react-redux';
import { createSelector } from 'reselect';
import createCollectionSelector from 'Store/Selectors/createCollectionSelector';

function createMapStateToProps() {
  return createSelector(createCollectionSelector(), (collection) => {
    // If a game is deleted this selector may fire before the parent
    // selectors, which will result in an undefined game, if that happens
    // we want to return early here and again in the render function to avoid
    // trying to show a game that has no information available.

    if (!collection) {
      return {};
    }

    const allGenres = collection.games.flatMap((game) => game.genres);

    return {
      ...collection,
      games: [...collection.games].sort((a, b) => b.year - a.year),
      genres: Array.from(new Set(allGenres)),
    };
  });
}

const connector = connect(createMapStateToProps);

type PropsFromRedux = ConnectedProps<typeof connector>;

interface CollectionItemConnectorProps extends PropsFromRedux {
  id?: number;
  collectionId: number;
  component: ComponentType<{ id: number; [key: string]: unknown }>;
  [key: string]: unknown;
}

class CollectionItemConnector extends Component<CollectionItemConnectorProps> {
  //
  // Render

  render() {
    const { id, component: ItemComponent, ...otherProps } = this.props;

    if (!id) {
      return null;
    }

    return <ItemComponent {...otherProps} id={id} />;
  }
}

export default connector(CollectionItemConnector);
