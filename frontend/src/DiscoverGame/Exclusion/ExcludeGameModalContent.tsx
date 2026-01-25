import { useCallback } from 'react';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ExcludeGameModalContent.css';

interface ExcludeGameModalContentProps {
  igdbId: number;
  title: string;
  onExcludePress: () => void;
  onModalClose: () => void;
}

function ExcludeGameModalContent({
  igdbId,
  title,
  onExcludePress,
  onModalClose,
}: ExcludeGameModalContentProps) {
  const handleExcludeGameConfirmed = useCallback(() => {
    onExcludePress();
  }, [onExcludePress]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        Exclude - {title} ({igdbId})
      </ModalHeader>

      <ModalBody>
        <div className={styles.pathContainer}>
          {translate('ExcludeTitle', { title })}
        </div>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>

        <Button kind={kinds.DANGER} onPress={handleExcludeGameConfirmed}>
          Exclude
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default ExcludeGameModalContent;
