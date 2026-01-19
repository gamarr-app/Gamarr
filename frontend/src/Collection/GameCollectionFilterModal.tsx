import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setGameCollectionsFilter } from 'Store/Actions/gameCollectionActions';

interface GameCollectionFilterModalProps {
  isOpen: boolean;
}

export default function GameCollectionFilterModal(
  props: GameCollectionFilterModalProps
) {
  const sectionItems = useSelector(
    (state: AppState) => state.gameCollections.items
  );
  const filterBuilderProps = useSelector(
    (state: AppState) => state.gameCollections.filterBuilderProps
  );

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: { selectedFilterKey: string | number }) => {
      dispatch(setGameCollectionsFilter(payload));
    },
    [dispatch]
  );

  return (
    <FilterModal
      {...props}
      sectionItems={sectionItems}
      filterBuilderProps={filterBuilderProps}
      customFilterType="gameCollections"
      dispatchSetFilter={dispatchSetFilter}
    />
  );
}
