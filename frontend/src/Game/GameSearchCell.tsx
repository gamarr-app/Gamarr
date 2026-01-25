import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { GAME_SEARCH } from 'Commands/commandNames';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { GameEntity } from 'Game/useGame';
import useModalOpenState from 'Helpers/Hooks/useModalOpenState';
import { icons } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createExecutingCommandsSelector from 'Store/Selectors/createExecutingCommandsSelector';
import translate from 'Utilities/String/translate';
import GameInteractiveSearchModal from './Search/GameInteractiveSearchModal';
import styles from './GameSearchCell.css';

interface GameSearchCellProps {
  gameId: number;
  gameEntity?: GameEntity;
}

function GameSearchCell({ gameId }: GameSearchCellProps) {
  const executingCommands = useSelector(createExecutingCommandsSelector());
  const isSearching = executingCommands.some(({ name, body }) => {
    const { gameIds = [] } = body;
    return name === GAME_SEARCH && gameIds.indexOf(gameId) > -1;
  });

  const dispatch = useDispatch();

  const [
    isInteractiveSearchModalOpen,
    setInteractiveSearchModalOpen,
    setInteractiveSearchModalClosed,
  ] = useModalOpenState(false);

  const handleSearchPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: GAME_SEARCH,
        gameIds: [gameId],
      })
    );
  }, [gameId, dispatch]);

  return (
    <TableRowCell className={styles.gameSearchCell}>
      <SpinnerIconButton
        name={icons.SEARCH}
        isSpinning={isSearching}
        title={translate('AutomaticSearch')}
        onPress={handleSearchPress}
      />

      <IconButton
        name={icons.INTERACTIVE}
        title={translate('InteractiveSearch')}
        onPress={setInteractiveSearchModalOpen}
      />

      <GameInteractiveSearchModal
        isOpen={isInteractiveSearchModalOpen}
        gameId={gameId}
        onModalClose={setInteractiveSearchModalClosed}
      />
    </TableRowCell>
  );
}

export default GameSearchCell;
