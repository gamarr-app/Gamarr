import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditRemotePathMappingModalContent from './EditRemotePathMappingModalContent';

interface EditRemotePathMappingModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
  onDeleteRemotePathMappingPress?: () => void;
}

function EditRemotePathMappingModal({
  id,
  isOpen,
  onModalClose,
}: EditRemotePathMappingModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.remotePathMappings' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={handleModalClose}>
      <EditRemotePathMappingModalContent
        id={id}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default EditRemotePathMappingModal;
