import { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { fetchIndexers } from 'Store/Actions/settingsActions';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

function createIndexersSelector() {
  return createSelector(
    (state: AppState) => state.settings.indexers,
    (indexers) => {
      const { isPopulated, items } = indexers;

      const tagList = items.map((item) => {
        return {
          id: item.id,
          name: item.name,
        };
      });

      return {
        isPopulated,
        tagList,
      };
    }
  );
}

function IndexerFilterBuilderRowValueConnector(
  props: FilterBuilderRowValueProps
) {
  const dispatch = useDispatch();
  const { isPopulated, tagList } = useSelector(createIndexersSelector());

  useEffect(() => {
    if (!isPopulated) {
      dispatch(fetchIndexers());
    }
  }, [isPopulated, dispatch]);

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default IndexerFilterBuilderRowValueConnector;
