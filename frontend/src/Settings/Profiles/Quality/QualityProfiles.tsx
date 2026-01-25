import React, { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons } from 'Helpers/Props';
import { fetchGameCollections } from 'Store/Actions/gameCollectionActions';
import {
  cloneQualityProfile,
  deleteQualityProfile,
  fetchQualityProfiles,
} from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import translate from 'Utilities/String/translate';
import EditQualityProfileModal from './EditQualityProfileModal';
import QualityProfile from './QualityProfile';
import styles from './QualityProfiles.css';

interface QualityProfileListItem {
  id: number;
  name: string;
  upgradeAllowed: boolean;
  cutoff: number;
  items: Array<{
    id: number;
    name: string;
    allowed: boolean;
    quality?: { id: number; name: string };
    items?: Array<{ quality: { id: number; name: string } }>;
  }>;
}

function QualityProfiles() {
  const dispatch = useDispatch();
  const { isFetching, isPopulated, error, items, isDeleting } = useSelector(
    createSortedSectionSelector(
      'settings.qualityProfiles',
      (a: { id: number; name: string }, b: { id: number; name: string }) =>
        a.name.localeCompare(b.name)
    )
  ) as unknown as {
    isFetching: boolean;
    isPopulated: boolean;
    error: import('App/State/AppSectionState').Error | undefined;
    items: QualityProfileListItem[];
    isDeleting: boolean;
  };

  const [isQualityProfileModalOpen, setIsQualityProfileModalOpen] =
    useState(false);

  useEffect(() => {
    dispatch(fetchQualityProfiles());
    dispatch(fetchGameCollections());
  }, [dispatch]);

  const handleConfirmDeleteQualityProfile = useCallback(
    (id: number) => {
      dispatch(deleteQualityProfile({ id }));
    },
    [dispatch]
  );

  const handleCloneQualityProfilePress = useCallback(
    (id: number) => {
      dispatch(cloneQualityProfile({ id }));
      setIsQualityProfileModalOpen(true);
    },
    [dispatch]
  );

  const handleEditQualityProfilePress = useCallback(() => {
    setIsQualityProfileModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsQualityProfileModalOpen(false);
  }, []);

  return (
    <FieldSet legend={translate('QualityProfiles')}>
      <PageSectionContent
        errorMessage={translate('QualityProfilesLoadError')}
        isFetching={isFetching}
        isPopulated={isPopulated}
        error={error}
      >
        <div className={styles.qualityProfiles}>
          {items.map((item) => {
            return (
              <QualityProfile
                key={item.id}
                {...item}
                isDeleting={isDeleting}
                onConfirmDeleteQualityProfile={
                  handleConfirmDeleteQualityProfile
                }
                onCloneQualityProfilePress={handleCloneQualityProfilePress}
              />
            );
          })}

          <Card
            className={styles.addQualityProfile}
            onPress={handleEditQualityProfilePress}
          >
            <div className={styles.center}>
              <Icon name={icons.ADD} size={45} />
            </div>
          </Card>
        </div>

        <EditQualityProfileModal
          isOpen={isQualityProfileModalOpen}
          onModalClose={handleModalClose}
        />
      </PageSectionContent>
    </FieldSet>
  );
}

export default QualityProfiles;
