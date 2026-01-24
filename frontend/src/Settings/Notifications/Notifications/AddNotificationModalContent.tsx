import React, { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import {
  fetchNotificationSchema,
  selectNotificationSchema,
} from 'Store/Actions/settingsActions';
import translate from 'Utilities/String/translate';
import AddNotificationItem from './AddNotificationItem';
import styles from './AddNotificationModalContent.css';

interface AddNotificationModalContentProps {
  onModalClose: (options?: { notificationSelected?: boolean }) => void;
}

function AddNotificationModalContent({
  onModalClose,
}: AddNotificationModalContentProps) {
  const dispatch = useDispatch();

  const { isSchemaFetching, isSchemaPopulated, schemaError, schema } =
    useSelector((state: AppState) => state.settings.notifications);

  useEffect(() => {
    dispatch(fetchNotificationSchema());
  }, [dispatch]);

  const handleClosePress = useCallback(() => {
    onModalClose();
  }, [onModalClose]);

  const handleNotificationSelect = useCallback(
    ({ implementation, name }: { implementation: string; name?: string }) => {
      dispatch(selectNotificationSchema({ implementation, presetName: name }));
      onModalClose({ notificationSelected: true });
    },
    [dispatch, onModalClose]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('AddConnection')}</ModalHeader>

      <ModalBody>
        {isSchemaFetching ? <LoadingIndicator /> : null}

        {!isSchemaFetching && !!schemaError ? (
          <Alert kind={kinds.DANGER}>{translate('AddNotificationError')}</Alert>
        ) : null}

        {isSchemaPopulated && !schemaError ? (
          <div>
            <div className={styles.notifications}>
              {schema.map((notification: any) => {
                return (
                  <AddNotificationItem
                    key={notification.implementation}
                    {...notification}
                    onNotificationSelect={handleNotificationSelect}
                  />
                );
              })}
            </div>
          </div>
        ) : null}
      </ModalBody>
      <ModalFooter>
        <Button onPress={handleClosePress}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default AddNotificationModalContent;
