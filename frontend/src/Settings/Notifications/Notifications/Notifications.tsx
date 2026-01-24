import React, { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons } from 'Helpers/Props';
import {
  deleteNotification,
  fetchNotifications,
} from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import translate from 'Utilities/String/translate';
import AddNotificationModal from './AddNotificationModal';
import EditNotificationModal from './EditNotificationModal';
import Notification from './Notification';
import styles from './Notifications.css';

function createNotificationsSelector() {
  return createSelector(
    createSortedSectionSelector(
      'settings.notifications',
      (a: { id: number; name: string }, b: { id: number; name: string }) =>
        a.name.localeCompare(b.name)
    ),
    createTagsSelector(),
    (notifications, tagList) => {
      return {
        ...notifications,
        tagList,
      };
    }
  );
}

function Notifications() {
  const dispatch = useDispatch();
  const { isFetching, error, items, tagList, isPopulated } = useSelector(
    createNotificationsSelector()
  );

  const [isAddNotificationModalOpen, setIsAddNotificationModalOpen] =
    useState(false);
  const [isEditNotificationModalOpen, setIsEditNotificationModalOpen] =
    useState(false);

  useEffect(() => {
    dispatch(fetchNotifications());
  }, [dispatch]);

  const handleAddNotificationPress = useCallback(() => {
    setIsAddNotificationModalOpen(true);
  }, []);

  const handleAddNotificationModalClose = useCallback(
    ({ notificationSelected = false } = {}) => {
      setIsAddNotificationModalOpen(false);
      setIsEditNotificationModalOpen(notificationSelected);
    },
    []
  );

  const handleEditNotificationModalClose = useCallback(() => {
    setIsEditNotificationModalOpen(false);
  }, []);

  const handleConfirmDeleteNotification = useCallback(
    (id: number) => {
      dispatch(deleteNotification({ id }));
    },
    [dispatch]
  );

  return (
    <FieldSet legend={translate('Connections')}>
      <PageSectionContent
        errorMessage={translate('NotificationsLoadError')}
        isFetching={isFetching}
        isPopulated={isPopulated}
        error={error}
      >
        <div className={styles.notifications}>
          {items.map((item: { id: number }) => {
            return (
              <Notification
                key={item.id}
                {...item}
                tagList={tagList}
                onConfirmDeleteNotification={handleConfirmDeleteNotification}
              />
            );
          })}

          <Card
            className={styles.addNotification}
            onPress={handleAddNotificationPress}
          >
            <div className={styles.center}>
              <Icon name={icons.ADD} size={45} />
            </div>
          </Card>
        </div>

        <AddNotificationModal
          isOpen={isAddNotificationModalOpen}
          onModalClose={handleAddNotificationModalClose}
        />

        <EditNotificationModal
          isOpen={isEditNotificationModalOpen}
          onModalClose={handleEditNotificationModalClose}
        />
      </PageSectionContent>
    </FieldSet>
  );
}

export default Notifications;
