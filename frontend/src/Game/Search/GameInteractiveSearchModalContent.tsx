import { useEffect } from 'react';
import { useDispatch } from 'react-redux';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Game from 'Game/Game';
import useGame from 'Game/useGame';
import { scrollDirections } from 'Helpers/Props';
import InteractiveSearch from 'InteractiveSearch/InteractiveSearch';
import { clearGameBlocklist } from 'Store/Actions/gameBlocklistActions';
import { clearGameHistory } from 'Store/Actions/gameHistoryActions';
import {
  cancelFetchReleases,
  clearReleases,
} from 'Store/Actions/releaseActions';
import translate from 'Utilities/String/translate';

export interface GameInteractiveSearchModalContentProps {
  gameId: number;
  componentId?: number;
  componentTitle?: string;
  onModalClose(): void;
}

function GameInteractiveSearchModalContent({
  gameId,
  componentId,
  componentTitle,
  onModalClose,
}: GameInteractiveSearchModalContentProps) {
  const dispatch = useDispatch();

  const { title, year } = useGame(gameId) as Game;

  useEffect(() => {
    return () => {
      dispatch(cancelFetchReleases());
      dispatch(clearReleases());

      dispatch(clearGameBlocklist());
      dispatch(clearGameHistory());
    };
  }, [dispatch]);

  const gameTitle = `${title}${year > 0 ? ` (${year})` : ''}`;
  const modalTitle = componentTitle
    ? `${gameTitle} - ${componentTitle}`
    : gameTitle;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {modalTitle
          ? translate('InteractiveSearchModalHeaderTitle', {
              title: modalTitle,
            })
          : translate('InteractiveSearchModalHeader')}
      </ModalHeader>

      <ModalBody scrollDirection={scrollDirections.BOTH}>
        <InteractiveSearch searchPayload={{ gameId, componentId }} />
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default GameInteractiveSearchModalContent;
