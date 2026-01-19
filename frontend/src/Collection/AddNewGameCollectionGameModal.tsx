import React, { useCallback, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import usePrevious from 'Helpers/Hooks/usePrevious';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import AddNewGameCollectionGameModalContent, {
  AddNewGameCollectionGameModalContentProps,
} from './AddNewGameCollectionGameModalContent';

interface AddNewCollectionGameModalProps
  extends AddNewGameCollectionGameModalContentProps {
  isOpen: boolean;
}

function AddNewGameCollectionGameModal({
  isOpen,
  onModalClose,
  ...otherProps
}: AddNewCollectionGameModalProps) {
  const dispatch = useDispatch();

  const wasOpen = usePrevious(isOpen);

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'gameCollections' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  useEffect(() => {
    if (wasOpen && !isOpen) {
      dispatch(clearPendingChanges({ section: 'gameCollections' }));
    }
  }, [wasOpen, isOpen, dispatch]);

  return (
    <Modal isOpen={isOpen} onModalClose={handleModalClose}>
      <AddNewGameCollectionGameModalContent
        {...otherProps}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default AddNewGameCollectionGameModal;
