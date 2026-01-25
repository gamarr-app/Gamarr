import React, { useCallback, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useSelect } from 'App/SelectContext';
import { GAME_SEARCH } from 'Commands/commandNames';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import { icons, kinds } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createGameClientSideCollectionItemsSelector from 'Store/Selectors/createGameClientSideCollectionItemsSelector';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';

interface GameIndexSearchButtonProps {
  isSelectMode: boolean;
  selectedFilterKey: string;
  overflowComponent: React.FunctionComponent<never>;
}

function GameIndexSearchButton(props: GameIndexSearchButtonProps) {
  const isSearching = useSelector(createCommandExecutingSelector(GAME_SEARCH));
  const { items } = useSelector(
    createGameClientSideCollectionItemsSelector('gameIndex')
  );

  const dispatch = useDispatch();
  const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false);

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
    setIsConfirmModalOpen(false);

    dispatch(
      executeCommand({
        name: GAME_SEARCH,
        gameIds: gamesToSearch,
      })
    );
  }, [dispatch, gamesToSearch]);

  const onConfirmPress = useCallback(() => {
    setIsConfirmModalOpen(true);
  }, [setIsConfirmModalOpen]);

  const onConfirmModalClose = useCallback(() => {
    setIsConfirmModalOpen(false);
  }, [setIsConfirmModalOpen]);

  return (
    <>
      <PageToolbarButton
        label={isSelectMode ? searchSelectLabel : searchIndexLabel}
        isSpinning={isSearching}
        isDisabled={!items.length}
        iconName={icons.SEARCH}
        onPress={gamesToSearch.length > 5 ? onConfirmPress : onPress}
      />

      <ConfirmModal
        isOpen={isConfirmModalOpen}
        kind={kinds.DANGER}
        title={isSelectMode ? searchSelectLabel : searchIndexLabel}
        message={translate('SearchGamesConfirmationMessageText', {
          count: gamesToSearch.length,
        })}
        confirmLabel={isSelectMode ? searchSelectLabel : searchIndexLabel}
        onConfirm={onPress}
        onCancel={onConfirmModalClose}
      />
    </>
  );
}

export default GameIndexSearchButton;
