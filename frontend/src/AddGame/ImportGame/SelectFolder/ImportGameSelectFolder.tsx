import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import FileBrowserModal from 'Components/FileBrowser/FileBrowserModal';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import ImportGameRootFolderRowConnector from './ImportGameRootFolderRowConnector';
import styles from './ImportGameSelectFolder.css';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface RootFolderItem {
  id: number;
  path: string;
  freeSpace: number;
  unmappedFolders: UnmappedFolder[];
}

interface SaveError {
  responseJSON?: Array<{ errorMessage: string }> | object;
}

interface ImportGameSelectFolderProps {
  isWindows: boolean;
  isFetching: boolean;
  isPopulated: boolean;
  isSaving: boolean;
  error?: object;
  saveError?: SaveError;
  items: RootFolderItem[];
  onNewRootFolderSelect: (path: string) => void;
  onDeleteRootFolderPress: (id: number) => void;
}

interface ImportGameSelectFolderState {
  isAddNewRootFolderModalOpen: boolean;
}

const rootFolderColumns = [
  {
    name: 'path',
    label: () => translate('Path'),
    isVisible: true,
  },
  {
    name: 'freeSpace',
    label: () => translate('FreeSpace'),
    isVisible: true,
  },
  {
    name: 'unmappedFolders',
    label: () => translate('UnmappedFolders'),
    isVisible: true,
  },
  {
    name: 'actions',
    label: () => '',
    isVisible: true,
  },
];

class ImportGameSelectFolder extends Component<
  ImportGameSelectFolderProps,
  ImportGameSelectFolderState
> {
  //
  // Lifecycle

  constructor(props: ImportGameSelectFolderProps) {
    super(props);

    this.state = {
      isAddNewRootFolderModalOpen: false,
    };
  }

  //
  // Lifecycle

  onAddNewRootFolderPress = () => {
    this.setState({ isAddNewRootFolderModalOpen: true });
  };

  onNewRootFolderSelect = ({ value }: { value: string }) => {
    this.props.onNewRootFolderSelect(value);
  };

  onAddRootFolderModalClose = () => {
    this.setState({ isAddNewRootFolderModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      isWindows,
      isFetching,
      isPopulated,
      isSaving,
      error,
      saveError,
      items,
    } = this.props;

    const hasRootFolders = items.length > 0;

    return (
      <PageContent title={translate('ImportGames')}>
        <PageContentBody>
          {isFetching && !isPopulated ? <LoadingIndicator /> : null}

          {!isFetching && error ? (
            <Alert kind={kinds.DANGER}>
              {translate('RootFoldersLoadError')}
            </Alert>
          ) : null}

          {!error && isPopulated && (
            <div>
              <div className={styles.header}>{translate('ImportHeader')}</div>

              <div className={styles.tips}>
                {translate('ImportTipsMessage')}
                <ul>
                  <li
                    dangerouslySetInnerHTML={{
                      __html: translate('ImportIncludeQuality', {
                        0: '<code>game.2008.bluray.mkv</code>',
                      }),
                    }}
                    className={styles.tip}
                  />
                  <li
                    dangerouslySetInnerHTML={{
                      __html: translate('ImportRootPath', {
                        0: `<code>${isWindows ? 'C:\\games' : '/games'}</code>`,
                        1: `<code>${
                          isWindows
                            ? 'C:\\games\\the matrix'
                            : '/games/the matrix'
                        }</code>`,
                      }),
                    }}
                    className={styles.tip}
                  />
                  <li className={styles.tip}>
                    {translate('ImportNotForDownloads')}
                  </li>
                </ul>
              </div>

              {hasRootFolders ? (
                <div className={styles.recentFolders}>
                  <FieldSet legend={translate('RecentFolders')}>
                    <Table columns={rootFolderColumns}>
                      <TableBody>
                        {items.map((rootFolder) => {
                          return (
                            <ImportGameRootFolderRowConnector
                              key={rootFolder.id}
                              id={rootFolder.id}
                              path={rootFolder.path}
                              freeSpace={rootFolder.freeSpace}
                              unmappedFolders={rootFolder.unmappedFolders}
                            />
                          );
                        })}
                      </TableBody>
                    </Table>
                  </FieldSet>
                </div>
              ) : null}

              {!isSaving && saveError ? (
                <Alert className={styles.addErrorAlert} kind={kinds.DANGER}>
                  {translate('AddRootFolderError')}

                  <ul>
                    {Array.isArray(saveError.responseJSON) ? (
                      saveError.responseJSON.map((e, index) => {
                        return <li key={index}>{e.errorMessage}</li>;
                      })
                    ) : (
                      <li>{JSON.stringify(saveError.responseJSON)}</li>
                    )}
                  </ul>
                </Alert>
              ) : null}

              <div className={hasRootFolders ? undefined : styles.startImport}>
                <Button
                  kind={kinds.PRIMARY}
                  size={sizes.LARGE}
                  onPress={this.onAddNewRootFolderPress}
                >
                  <Icon
                    className={styles.importButtonIcon}
                    name={icons.DRIVE}
                  />
                  {hasRootFolders
                    ? translate('ChooseAnotherFolder')
                    : translate('StartImport')}
                </Button>
              </div>

              <FileBrowserModal
                isOpen={this.state.isAddNewRootFolderModalOpen}
                name="rootFolderPath"
                value=""
                onChange={this.onNewRootFolderSelect}
                onModalClose={this.onAddRootFolderModalClose}
              />
            </div>
          )}
        </PageContentBody>
      </PageContent>
    );
  }
}

export default ImportGameSelectFolder;
