import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearGameBlocklist } from 'Store/Actions/gameBlocklistActions';
import { clearGameHistory } from 'Store/Actions/gameHistoryActions';
import {
  cancelFetchReleases,
  clearReleases,
} from 'Store/Actions/releaseActions';
import GameInteractiveSearchModalContent, {
  GameInteractiveSearchModalContentProps,
} from './GameInteractiveSearchModalContent';

interface GameInteractiveSearchModalProps
  extends GameInteractiveSearchModalContentProps {
  isOpen: boolean;
}

function GameInteractiveSearchModal({
  isOpen,
  gameId,
  onModalClose,
}: GameInteractiveSearchModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(cancelFetchReleases());
    dispatch(clearReleases());

    dispatch(clearGameBlocklist());
    dispatch(clearGameHistory());

    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal
      isOpen={isOpen}
      closeOnBackgroundClick={false}
      size={sizes.EXTRA_EXTRA_LARGE}
      onModalClose={handleModalClose}
    >
      <GameInteractiveSearchModalContent
        gameId={gameId}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default GameInteractiveSearchModal;
