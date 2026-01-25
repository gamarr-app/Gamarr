import _ from 'lodash';
import { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import {
  fetchDownloadClientSchema,
  selectDownloadClientSchema,
} from 'Store/Actions/settingsActions';
import translate from 'Utilities/String/translate';
import AddDownloadClientItem from './AddDownloadClientItem';
import styles from './AddDownloadClientModalContent.css';

interface AddDownloadClientModalContentProps {
  onModalClose: (options?: { downloadClientSelected?: boolean }) => void;
}

function AddDownloadClientModalContent({
  onModalClose,
}: AddDownloadClientModalContentProps) {
  const dispatch = useDispatch();

  const { isSchemaFetching, isSchemaPopulated, schemaError, schema } =
    useSelector(
      (state: {
        settings: {
          downloadClients: {
            isSchemaFetching: boolean;
            isSchemaPopulated: boolean;
            schemaError: object | null;
            schema: Array<{
              protocol: string;
              implementation: string;
              implementationName: string;
              infoLink: string;
            }>;
          };
        };
      }) => state.settings.downloadClients
    );

  const usenetDownloadClients = _.filter(schema, { protocol: 'usenet' });
  const torrentDownloadClients = _.filter(schema, { protocol: 'torrent' });

  useEffect(() => {
    dispatch(fetchDownloadClientSchema());
  }, [dispatch]);

  const handleDownloadClientSelect = useCallback(
    ({ implementation }: { implementation: string }) => {
      dispatch(selectDownloadClientSchema({ implementation }));
      onModalClose({ downloadClientSelected: true });
    },
    [dispatch, onModalClose]
  );

  const handleClose = useCallback(() => {
    onModalClose();
  }, [onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('AddDownloadClient')}</ModalHeader>

      <ModalBody>
        {isSchemaFetching ? <LoadingIndicator /> : null}

        {!isSchemaFetching && !!schemaError ? (
          <Alert kind={kinds.DANGER}>
            {translate('AddDownloadClientError')}
          </Alert>
        ) : null}

        {isSchemaPopulated && !schemaError ? (
          <div>
            <Alert kind={kinds.INFO}>
              <div>{translate('SupportedDownloadClients')}</div>
              <div>{translate('SupportedDownloadClientsMoreInfo')}</div>
            </Alert>

            <FieldSet legend={translate('Usenet')}>
              <div className={styles.downloadClients}>
                {usenetDownloadClients.map((downloadClient) => {
                  return (
                    <AddDownloadClientItem
                      key={downloadClient.implementation}
                      implementation={downloadClient.implementation}
                      implementationName={downloadClient.implementationName}
                      infoLink={downloadClient.infoLink}
                      onDownloadClientSelect={handleDownloadClientSelect}
                    />
                  );
                })}
              </div>
            </FieldSet>

            <FieldSet legend={translate('Torrents')}>
              <div className={styles.downloadClients}>
                {torrentDownloadClients.map((downloadClient) => {
                  return (
                    <AddDownloadClientItem
                      key={downloadClient.implementation}
                      implementation={downloadClient.implementation}
                      implementationName={downloadClient.implementationName}
                      infoLink={downloadClient.infoLink}
                      onDownloadClientSelect={handleDownloadClientSelect}
                    />
                  );
                })}
              </div>
            </FieldSet>
          </div>
        ) : null}
      </ModalBody>
      <ModalFooter>
        <Button onPress={handleClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default AddDownloadClientModalContent;
