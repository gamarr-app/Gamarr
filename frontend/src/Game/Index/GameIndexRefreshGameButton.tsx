import { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useSelect } from 'App/SelectContext';
import { REFRESH_GAME } from 'Commands/commandNames';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import { icons } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createGameClientSideCollectionItemsSelector from 'Store/Selectors/createGameClientSideCollectionItemsSelector';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';

interface GameIndexRefreshGameButtonProps {
  isSelectMode: boolean;
  selectedFilterKey: string;
}

function GameIndexRefreshGameButton(props: GameIndexRefreshGameButtonProps) {
  const isRefreshing = useSelector(
    createCommandExecutingSelector(REFRESH_GAME)
  );
  const { items, totalItems } = useSelector(
    createGameClientSideCollectionItemsSelector('gameIndex')
  );

  const dispatch = useDispatch();
  const { isSelectMode, selectedFilterKey } = props;
  const [selectState] = useSelect();
  const { selectedState } = selectState;

  const selectedGameIds = useMemo(() => {
    return getSelectedIds(selectedState);
  }, [selectedState]);

  const gamesToRefresh =
    isSelectMode && selectedGameIds.length > 0
      ? selectedGameIds
      : items.map((m) => m.id);

  const refreshIndexLabel =
    selectedFilterKey === 'all'
      ? translate('UpdateAll')
      : translate('UpdateFiltered');

  const refreshSelectLabel =
    selectedGameIds.length > 0
      ? translate('UpdateSelected')
      : translate('UpdateAll');

  const onPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: REFRESH_GAME,
        gameIds: gamesToRefresh,
      })
    );
  }, [dispatch, gamesToRefresh]);

  return (
    <PageToolbarButton
      label={isSelectMode ? refreshSelectLabel : refreshIndexLabel}
      isSpinning={isRefreshing}
      isDisabled={!totalItems}
      iconName={icons.REFRESH}
      onPress={onPress}
    />
  );
}

export default GameIndexRefreshGameButton;
