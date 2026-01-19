import { orderBy } from 'lodash';
import React, { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RENAME_GAME } from 'Commands/commandNames';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, kinds } from 'Helpers/Props';
import Game from 'Game/Game';
import { executeCommand } from 'Store/Actions/commandActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import translate from 'Utilities/String/translate';
import styles from './OrganizeGamesModalContent.css';

interface OrganizeGamesModalContentProps {
  gameIds: number[];
  onModalClose: () => void;
}

function OrganizeGamesModalContent(props: OrganizeGamesModalContentProps) {
  const { gameIds, onModalClose } = props;

  const allGames: Game[] = useSelector(createAllGamesSelector());
  const dispatch = useDispatch();

  const gameTitles = useMemo(() => {
    const game = gameIds.reduce((acc: Game[], id) => {
      const s = allGames.find((s) => s.id === id);

      if (s) {
        acc.push(s);
      }

      return acc;
    }, []);

    const sorted = orderBy(game, ['sortTitle']);

    return sorted.map((s) => s.title);
  }, [gameIds, allGames]);

  const onOrganizePress = useCallback(() => {
    dispatch(
      executeCommand({
        name: RENAME_GAME,
        gameIds,
      })
    );

    onModalClose();
  }, [gameIds, onModalClose, dispatch]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('OrganizeSelectedGames')}</ModalHeader>

      <ModalBody>
        <Alert>
          {translate('PreviewRenameHelpText')}
          <Icon className={styles.renameIcon} name={icons.ORGANIZE} />
        </Alert>

        <div className={styles.message}>
          {translate('OrganizeConfirm', { count: gameTitles.length })}
        </div>

        <ul>
          {gameTitles.map((title) => {
            return <li key={title}>{title}</li>;
          })}
        </ul>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.DANGER} onPress={onOrganizePress}>
          {translate('Organize')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default OrganizeGamesModalContent;
