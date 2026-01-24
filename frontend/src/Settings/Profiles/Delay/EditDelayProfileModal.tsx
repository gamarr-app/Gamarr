import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditDelayProfileModalContent from './EditDelayProfileModalContent';

interface EditDelayProfileModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
}

function EditDelayProfileModal({
  id,
  isOpen,
  onModalClose,
}: EditDelayProfileModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.delayProfiles' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={handleModalClose}>
      <EditDelayProfileModalContent
        id={id}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default EditDelayProfileModal;
