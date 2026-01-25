import { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { AppSectionProviderState, Error } from 'App/State/AppSectionState';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons } from 'Helpers/Props';
import {
  deleteDownloadClient,
  fetchDownloadClients,
} from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import DownloadClientType from 'typings/DownloadClient';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import AddDownloadClientModal from './AddDownloadClientModal';
import DownloadClient from './DownloadClient';
import EditDownloadClientModal from './EditDownloadClientModal';
import styles from './DownloadClients.css';

function createDownloadClientsSelector() {
  return createSelector(
    createSortedSectionSelector<
      DownloadClientType,
      AppSectionProviderState<DownloadClientType>
    >('settings.downloadClients', sortByProp('name')),
    createTagsSelector(),
    (downloadClients, tagList) => ({
      ...downloadClients,
      tagList,
    })
  );
}

function DownloadClients() {
  const dispatch = useDispatch();
  const { isFetching, isPopulated, error, items, tagList } = useSelector(
    createDownloadClientsSelector()
  ) as {
    isFetching: boolean;
    isPopulated: boolean;
    error: Error | undefined;
    items: DownloadClientType[];
    tagList: import('App/State/TagsAppState').Tag[];
  };

  const [isAddDownloadClientModalOpen, setIsAddDownloadClientModalOpen] =
    useState(false);
  const [isEditDownloadClientModalOpen, setIsEditDownloadClientModalOpen] =
    useState(false);

  useEffect(() => {
    dispatch(fetchDownloadClients());
  }, [dispatch]);

  const handleConfirmDeleteDownloadClient = useCallback(
    (id: number) => {
      dispatch(deleteDownloadClient({ id }));
    },
    [dispatch]
  );

  const handleAddDownloadClientPress = useCallback(() => {
    setIsAddDownloadClientModalOpen(true);
  }, []);

  const handleAddDownloadClientModalClose = useCallback(
    ({ downloadClientSelected = false } = {}) => {
      setIsAddDownloadClientModalOpen(false);
      setIsEditDownloadClientModalOpen(downloadClientSelected);
    },
    []
  );

  const handleEditDownloadClientModalClose = useCallback(() => {
    setIsEditDownloadClientModalOpen(false);
  }, []);

  return (
    <FieldSet legend={translate('DownloadClients')}>
      <PageSectionContent
        errorMessage={translate('DownloadClientsLoadError')}
        isFetching={isFetching}
        isPopulated={isPopulated}
        error={error}
      >
        <div className={styles.downloadClients}>
          {items.map((item) => {
            return (
              <DownloadClient
                key={item.id}
                {...item}
                tagList={tagList}
                onConfirmDeleteDownloadClient={
                  handleConfirmDeleteDownloadClient
                }
              />
            );
          })}

          <Card
            className={styles.addDownloadClient}
            onPress={handleAddDownloadClientPress}
          >
            <div className={styles.center}>
              <Icon name={icons.ADD} size={45} />
            </div>
          </Card>
        </div>

        <AddDownloadClientModal
          isOpen={isAddDownloadClientModalOpen}
          onModalClose={handleAddDownloadClientModalClose}
        />

        <EditDownloadClientModal
          isOpen={isEditDownloadClientModalOpen}
          onModalClose={handleEditDownloadClientModalClose}
        />
      </PageSectionContent>
    </FieldSet>
  );
}

export default DownloadClients;
