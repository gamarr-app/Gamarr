import React, { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Error as AppError } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import InlineMarkdown from 'Components/Markdown/InlineMarkdown';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons, kinds } from 'Helpers/Props';
import {
  deleteRemotePathMapping,
  fetchRemotePathMappings,
} from 'Store/Actions/settingsActions';
import translate from 'Utilities/String/translate';
import EditRemotePathMappingModal from './EditRemotePathMappingModal';
import RemotePathMapping from './RemotePathMapping';
import styles from './RemotePathMappings.css';

function RemotePathMappings() {
  const dispatch = useDispatch();
  const { isFetching, isPopulated, error, items } = useSelector(
    (state: {
      settings: {
        remotePathMappings: {
          isFetching: boolean;
          isPopulated: boolean;
          error: AppError | undefined;
          items: Array<{ id: number }>;
        };
      };
    }) => state.settings.remotePathMappings
  );

  const [isAddRemotePathMappingModalOpen, setIsAddRemotePathMappingModalOpen] =
    useState(false);

  useEffect(() => {
    dispatch(fetchRemotePathMappings());
  }, [dispatch]);

  const handleConfirmDeleteRemotePathMapping = useCallback(
    (id: number) => {
      dispatch(deleteRemotePathMapping({ id }));
    },
    [dispatch]
  );

  const handleAddRemotePathMappingPress = useCallback(() => {
    setIsAddRemotePathMappingModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsAddRemotePathMappingModalOpen(false);
  }, []);

  return (
    <FieldSet legend={translate('RemotePathMappings')}>
      <PageSectionContent
        errorMessage={translate('RemotePathMappingsLoadError')}
        isFetching={isFetching}
        isPopulated={isPopulated}
        error={error}
      >
        <Alert kind={kinds.INFO}>
          <InlineMarkdown
            data={translate('RemotePathMappingsInfo', {
              wikiLink:
                'https://wiki.servarr.com/gamarr/settings#remote-path-mappings',
            })}
          />
        </Alert>

        <div className={styles.remotePathMappingsHeader}>
          <div className={styles.host}>{translate('Host')}</div>
          <div className={styles.path}>{translate('RemotePath')}</div>
          <div className={styles.path}>{translate('LocalPath')}</div>
        </div>

        <div>
          {items.map((item: any, index: number) => {
            return (
              <RemotePathMapping
                key={item.id}
                {...item}
                index={index}
                onConfirmDeleteRemotePathMapping={
                  handleConfirmDeleteRemotePathMapping
                }
              />
            );
          })}
        </div>

        <div className={styles.addRemotePathMapping}>
          <Link
            className={styles.addButton}
            onPress={handleAddRemotePathMappingPress}
          >
            <Icon name={icons.ADD} />
          </Link>
        </div>

        <EditRemotePathMappingModal
          isOpen={isAddRemotePathMappingModalOpen}
          onModalClose={handleModalClose}
        />
      </PageSectionContent>
    </FieldSet>
  );
}

export default RemotePathMappings;
