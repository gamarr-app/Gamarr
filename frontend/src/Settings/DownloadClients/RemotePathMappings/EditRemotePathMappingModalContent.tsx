import React, { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { Error as AppError } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import {
  saveRemotePathMapping,
  setRemotePathMappingValue,
} from 'Store/Actions/settingsActions';
import selectSettings from 'Store/Selectors/selectSettings';
import { InputChanged } from 'typings/inputs';
import { Failure } from 'typings/pending';
import translate from 'Utilities/String/translate';
import styles from './EditRemotePathMappingModalContent.css';

const newRemotePathMapping: Record<string, string> = {
  host: '',
  remotePath: '',
  localPath: '',
};

const selectDownloadClientHosts = createSelector(
  (state: {
    settings: {
      downloadClients: {
        items: Array<{
          name: string;
          fields: Array<{ name: string; value: string }>;
        }>;
      };
    };
  }) => state.settings.downloadClients.items,
  (downloadClients) => {
    const hosts: Record<string, string[]> = downloadClients.reduce(
      (acc: Record<string, string[]>, downloadClient) => {
        const name = downloadClient.name;
        const host = downloadClient.fields.find((field) => {
          return field.name === 'host';
        });

        if (host) {
          const group = (acc[host.value] = acc[host.value] || []);
          group.push(name);
        }

        return acc;
      },
      {}
    );

    return Object.keys(hosts).map((host) => {
      return {
        key: host,
        value: host,
        hint: `${hosts[host].join(', ')}`,
      };
    });
  }
);

function createRemotePathMappingSelector(id: number | undefined) {
  return createSelector(
    (state: {
      settings: {
        remotePathMappings: {
          isFetching: boolean;
          error: AppError | undefined;
          isSaving: boolean;
          saveError: AppError | undefined;
          pendingChanges: object;
          items: Array<{ id: number }>;
        };
      };
    }) => state.settings.remotePathMappings,
    selectDownloadClientHosts,
    (remotePathMappings, downloadClientHosts) => {
      const { isFetching, error, isSaving, saveError, pendingChanges, items } =
        remotePathMappings;

      const mapping = id
        ? items.find((i) => i.id === id)
        : newRemotePathMapping;
      const settings = selectSettings(
        mapping as unknown as Record<string, unknown>,
        pendingChanges,
        saveError
      );

      return {
        id,
        isFetching,
        error,
        isSaving,
        saveError,
        item: settings.settings,
        ...settings,
        downloadClientHosts,
      };
    }
  );
}

interface EditRemotePathMappingModalContentProps {
  id?: number;
  onModalClose: () => void;
  onDeleteRemotePathMappingPress?: () => void;
}

function EditRemotePathMappingModalContent({
  id,
  onModalClose,
  onDeleteRemotePathMappingPress,
}: EditRemotePathMappingModalContentProps) {
  const dispatch = useDispatch();

  const {
    isFetching,
    error,
    isSaving,
    saveError,
    item,
    downloadClientHosts,
    id: _id,
    ...otherSettings
  } = useSelector(createRemotePathMappingSelector(id)) as {
    isFetching: boolean;
    error: AppError | undefined;
    isSaving: boolean;
    saveError: AppError | undefined;
    item: Record<
      string,
      { value: string; errors?: Failure[]; warnings?: Failure[] }
    >;
    downloadClientHosts: Array<{ key: string; value: string; hint: string }>;
    id: number | undefined;
    [key: string]: unknown;
  };

  const prevIsSaving = useRef(isSaving);

  useEffect(() => {
    if (!id) {
      Object.keys(newRemotePathMapping).forEach((name) => {
        dispatch(
          // @ts-expect-error - actions aren't typed
          setRemotePathMappingValue({
            name,
            value: newRemotePathMapping[name],
          })
        );
      });
    }
  }, [dispatch, id]);

  useEffect(() => {
    if (prevIsSaving.current && !isSaving && !saveError) {
      onModalClose();
    }
    prevIsSaving.current = isSaving;
  }, [isSaving, saveError, onModalClose]);

  const handleInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      // @ts-expect-error - actions aren't typed
      dispatch(setRemotePathMappingValue({ name, value }));
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveRemotePathMapping({ id }));
  }, [dispatch, id]);

  const { host, remotePath, localPath } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id
          ? translate('EditRemotePathMapping')
          : translate('AddRemotePathMapping')}
      </ModalHeader>

      <ModalBody className={styles.body}>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <Alert kind={kinds.DANGER}>
            {translate('AddRemotePathMappingError')}
          </Alert>
        ) : null}

        {!isFetching && !error ? (
          <Form {...otherSettings}>
            <FormGroup>
              <FormLabel>{translate('Host')}</FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="host"
                helpText={translate('RemotePathMappingHostHelpText')}
                {...host}
                values={downloadClientHosts}
                onChange={handleInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('RemotePath')}</FormLabel>

              <FormInputGroup
                type={inputTypes.TEXT}
                name="remotePath"
                helpText={translate('RemotePathMappingRemotePathHelpText')}
                {...remotePath}
                onChange={handleInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('LocalPath')}</FormLabel>

              <FormInputGroup
                type={inputTypes.PATH}
                name="localPath"
                helpText={translate('RemotePathMappingLocalPathHelpText')}
                includeFiles={false}
                {...localPath}
                onChange={handleInputChange}
              />
            </FormGroup>
          </Form>
        ) : null}
      </ModalBody>

      <ModalFooter>
        {id ? (
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteRemotePathMappingPress}
          >
            {translate('Delete')}
          </Button>
        ) : null}

        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={handleSavePress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default EditRemotePathMappingModalContent;
