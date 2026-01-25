import { useCallback, useState } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditSpecificationModal from './EditSpecificationModal';
import styles from './Specification.css';

interface SpecificationProps {
  id: number;
  implementation: string;
  implementationName: string;
  name: string;
  negate: boolean;
  required: boolean;
  fields: object[];
  onConfirmDeleteSpecification: (id: number) => void;
  onCloneSpecificationPress: (id: number) => void;
}

function Specification({
  id,
  implementationName,
  name,
  required,
  negate,
  onConfirmDeleteSpecification,
  onCloneSpecificationPress,
}: SpecificationProps) {
  const [isEditSpecificationModalOpen, setIsEditSpecificationModalOpen] =
    useState(false);
  const [isDeleteSpecificationModalOpen, setIsDeleteSpecificationModalOpen] =
    useState(false);

  const handleEditSpecificationPress = useCallback(() => {
    setIsEditSpecificationModalOpen(true);
  }, []);

  const handleEditSpecificationModalClose = useCallback(() => {
    setIsEditSpecificationModalOpen(false);
  }, []);

  const handleDeleteSpecificationPress = useCallback(() => {
    setIsEditSpecificationModalOpen(false);
    setIsDeleteSpecificationModalOpen(true);
  }, []);

  const handleDeleteSpecificationModalClose = useCallback(() => {
    setIsDeleteSpecificationModalOpen(false);
  }, []);

  const handleCloneSpecificationPress = useCallback(() => {
    onCloneSpecificationPress(id);
  }, [id, onCloneSpecificationPress]);

  const handleConfirmDeleteSpecification = useCallback(() => {
    onConfirmDeleteSpecification(id);
  }, [id, onConfirmDeleteSpecification]);

  return (
    <Card
      className={styles.customFormat}
      overlayContent={true}
      onPress={handleEditSpecificationPress}
    >
      <div className={styles.nameContainer}>
        <div className={styles.name}>{name}</div>

        <IconButton
          className={styles.cloneButton}
          title={translate('CloneCondition')}
          name={icons.CLONE}
          onPress={handleCloneSpecificationPress}
        />
      </div>

      <div className={styles.labels}>
        <Label kind={kinds.DEFAULT}>{implementationName}</Label>

        {negate && <Label kind={kinds.DANGER}>{translate('Negated')}</Label>}

        {required && (
          <Label kind={kinds.SUCCESS}>{translate('Required')}</Label>
        )}
      </div>

      <EditSpecificationModal
        id={id}
        isOpen={isEditSpecificationModalOpen}
        onModalClose={handleEditSpecificationModalClose}
        onDeleteSpecificationPress={handleDeleteSpecificationPress}
      />

      <ConfirmModal
        isOpen={isDeleteSpecificationModalOpen}
        kind={kinds.DANGER}
        title={translate('DeleteCondition')}
        message={translate('DeleteConditionMessageText', { name })}
        confirmLabel={translate('Delete')}
        onConfirm={handleConfirmDeleteSpecification}
        onCancel={handleDeleteSpecificationModalClose}
      />
    </Card>
  );
}

export default Specification;
