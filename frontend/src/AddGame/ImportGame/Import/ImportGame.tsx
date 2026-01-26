import { reduce } from 'lodash';
import { Component, createRef, RefObject } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { kinds } from 'Helpers/Props';
import { SelectStateInputProps } from 'typings/props';
import translate from 'Utilities/String/translate';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import ImportGameFooterConnector from './ImportGameFooterConnector';
import ImportGameTableConnector from './ImportGameTableConnector';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface ImportGameItem {
  id: string;
}

interface ImportGameProps {
  rootFolderId: number;
  path?: string;
  rootFoldersFetching: boolean;
  rootFoldersPopulated: boolean;
  rootFoldersError?: object;
  unmappedFolders: UnmappedFolder[];
  items: ImportGameItem[];
  onInputChange: (ids: string[], name: string, value: unknown) => void;
  onImportPress: (ids: string[]) => void;
}

interface SelectedState {
  [key: string]: boolean;
}

interface ImportGameState {
  allSelected: boolean;
  allUnselected: boolean;
  lastToggled: string | number | null;
  selectedState: SelectedState;
  contentBody: Element | null;
}

class ImportGame extends Component<ImportGameProps, ImportGameState> {
  static defaultProps = {
    unmappedFolders: [],
  };

  // eslint-disable-next-line react/sort-comp
  scrollerRef: RefObject<HTMLDivElement | null>;

  //
  // Lifecycle

  constructor(props: ImportGameProps) {
    super(props);

    this.scrollerRef = createRef();

    this.state = {
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {},
      contentBody: null,
    };
  }

  //
  // Listeners

  getSelectedIds = (): string[] => {
    return reduce(
      this.state.selectedState,
      (result: string[], value, id) => {
        if (value) {
          result.push(id);
        }

        return result;
      },
      []
    );
  };

  onSelectAllChange = ({ value }: { value: boolean }) => {
    // Only select non-dupes
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectedChange = ({ id, value, shiftKey }: SelectStateInputProps) => {
    this.setState((state) => {
      return toggleSelected(
        state,
        this.props.items,
        id,
        value ?? false,
        shiftKey ?? false
      );
    });
  };

  onRemoveSelectedStateItem = (id: string) => {
    this.setState((state) => {
      const selectedState = Object.assign({}, state.selectedState);
      delete selectedState[id];

      return {
        ...state,
        selectedState,
      };
    });
  };

  onInputChange = ({ name, value }: { name: string; value: unknown }) => {
    this.props.onInputChange(this.getSelectedIds(), name, value);
  };

  onImportPress = () => {
    this.props.onImportPress(this.getSelectedIds());
  };

  //
  // Render

  render() {
    const {
      rootFolderId,
      path,
      rootFoldersFetching,
      rootFoldersError,
      rootFoldersPopulated,
      unmappedFolders,
    } = this.props;

    const { allSelected, allUnselected, selectedState } = this.state;

    return (
      <PageContent title={translate('ImportGames')}>
        <PageContentBody ref={this.scrollerRef}>
          {rootFoldersFetching ? <LoadingIndicator /> : null}

          {!rootFoldersFetching && !!rootFoldersError ? (
            <Alert kind={kinds.DANGER}>
              {translate('RootFoldersLoadError')}
            </Alert>
          ) : null}

          {!rootFoldersError &&
          !rootFoldersFetching &&
          rootFoldersPopulated &&
          !unmappedFolders.length ? (
            <Alert kind={kinds.INFO}>
              {translate('AllGamesInPathHaveBeenImported', {
                path: path || '',
              })}
            </Alert>
          ) : null}

          {!rootFoldersError &&
          !rootFoldersFetching &&
          rootFoldersPopulated &&
          !!unmappedFolders.length &&
          this.scrollerRef.current ? (
            <ImportGameTableConnector
              rootFolderId={rootFolderId}
              unmappedFolders={unmappedFolders}
              allSelected={allSelected}
              allUnselected={allUnselected}
              selectedState={selectedState}
              scroller={this.scrollerRef.current}
              onSelectAllChange={this.onSelectAllChange}
              onSelectedChange={this.onSelectedChange}
              onRemoveSelectedStateItem={this.onRemoveSelectedStateItem}
            />
          ) : null}
        </PageContentBody>

        {!rootFoldersError &&
        !rootFoldersFetching &&
        !!unmappedFolders.length ? (
          <ImportGameFooterConnector
            selectedIds={this.getSelectedIds()}
            onInputChange={this.onInputChange}
            onImportPress={this.onImportPress}
          />
        ) : null}
      </PageContent>
    );
  }
}

export default ImportGame;
