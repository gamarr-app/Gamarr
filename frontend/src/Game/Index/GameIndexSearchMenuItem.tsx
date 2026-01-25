import { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useSelect } from 'App/SelectContext';
import { GAME_SEARCH } from 'Commands/commandNames';
import PageToolbarOverflowMenuItem from 'Components/Page/Toolbar/PageToolbarOverflowMenuItem';
import { icons } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createGameClientSideCollectionItemsSelector from 'Store/Selectors/createGameClientSideCollectionItemsSelector';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';

interface GameIndexSearchMenuItemProps {
  isSelectMode: boolean;
  selectedFilterKey: string;
}

function GameIndexSearchMenuItem(props: GameIndexSearchMenuItemProps) {
  const isSearching = useSelector(createCommandExecutingSelector(GAME_SEARCH));
  const { items } = useSelector(
    createGameClientSideCollectionItemsSelector('gameIndex')
  );

  const dispatch = useDispatch();

  const { isSelectMode, selectedFilterKey } = props;
  const [selectState] = useSelect();
  const { selectedState } = selectState;

  const selectedGameIds = useMemo(() => {
    return getSelectedIds(selectedState);
  }, [selectedState]);

  const gamesToSearch =
    isSelectMode && selectedGameIds.length > 0
      ? selectedGameIds
      : items.map((m) => m.id);

  const searchIndexLabel =
    selectedFilterKey === 'all'
      ? translate('SearchAll')
      : translate('SearchFiltered');

  const searchSelectLabel =
    selectedGameIds.length > 0
      ? translate('SearchSelected')
      : translate('SearchAll');

  const onPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: GAME_SEARCH,
        gameIds: gamesToSearch,
      })
    );
  }, [dispatch, gamesToSearch]);

  return (
    <PageToolbarOverflowMenuItem
      label={isSelectMode ? searchSelectLabel : searchIndexLabel}
      isSpinning={isSearching}
      isDisabled={!items.length}
      iconName={icons.SEARCH}
      onPress={onPress}
    />
  );
}

export default GameIndexSearchMenuItem;
