import { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Error as AppError } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons, kinds } from 'Helpers/Props';
import {
  deletePlatformRootFolder,
  fetchPlatformRootFolders,
  PlatformRootFolderItem,
} from 'Store/Actions/settingsActions';
import translate from 'Utilities/String/translate';
import EditPlatformRootFolderModal from './EditPlatformRootFolderModal';
import PlatformRootFolder from './PlatformRootFolder';
import styles from './PlatformRootFolders.css';

function PlatformRootFolders() {
  const dispatch = useDispatch();
  const { isFetching, isPopulated, error, items } = useSelector(
    (state: {
      settings: {
        platformRootFolders: {
          isFetching: boolean;
          isPopulated: boolean;
          error: AppError | undefined;
          items: PlatformRootFolderItem[];
        };
      };
    }) => state.settings.platformRootFolders
  );

  const [
    isAddPlatformRootFolderModalOpen,
    setIsAddPlatformRootFolderModalOpen,
  ] = useState(false);

  useEffect(() => {
    dispatch(fetchPlatformRootFolders());
  }, [dispatch]);

  const handleConfirmDeletePlatformRootFolder = useCallback(
    (id: number) => {
      dispatch(deletePlatformRootFolder({ id }));
    },
    [dispatch]
  );

  const handleAddPlatformRootFolderPress = useCallback(() => {
    setIsAddPlatformRootFolderModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsAddPlatformRootFolderModalOpen(false);
  }, []);

  return (
    <FieldSet legend={translate('PlatformRootFolders')}>
      <PageSectionContent
        errorMessage={translate('PlatformRootFoldersLoadError')}
        isFetching={isFetching}
        isPopulated={isPopulated}
        error={error}
      >
        <Alert kind={kinds.INFO}>{translate('PlatformRootFoldersInfo')}</Alert>

        <div className={styles.platformRootFoldersHeader}>
          <div className={styles.platform}>{translate('Platform')}</div>
          <div className={styles.path}>{translate('RootFolder')}</div>
        </div>

        <div>
          {items.map((item) => {
            return (
              <PlatformRootFolder
                key={item.id}
                {...item}
                onConfirmDeletePlatformRootFolder={
                  handleConfirmDeletePlatformRootFolder
                }
              />
            );
          })}
        </div>

        <div className={styles.addPlatformRootFolder}>
          <Link
            className={styles.addButton}
            onPress={handleAddPlatformRootFolderPress}
          >
            <Icon name={icons.ADD} />
          </Link>
        </div>

        <EditPlatformRootFolderModal
          isOpen={isAddPlatformRootFolderModalOpen}
          onModalClose={handleModalClose}
        />
      </PageSectionContent>
    </FieldSet>
  );
}

export default PlatformRootFolders;
