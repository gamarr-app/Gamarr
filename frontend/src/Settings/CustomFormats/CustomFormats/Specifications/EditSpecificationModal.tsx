import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditSpecificationModalContentConnector from './EditSpecificationModalContentConnector';

interface EditSpecificationModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
}

function EditSpecificationModal({
  id,
  isOpen,
  onModalClose,
}: EditSpecificationModalProps) {
  const dispatch = useDispatch();

  const handleModalClose = useCallback(() => {
    dispatch(
      clearPendingChanges({ section: 'settings.customFormatSpecifications' })
    );
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={handleModalClose}>
      <EditSpecificationModalContentConnector
        id={id}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default EditSpecificationModal;
