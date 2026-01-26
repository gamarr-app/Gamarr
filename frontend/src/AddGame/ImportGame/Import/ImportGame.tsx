import { reduce } from 'lodash';
import { useCallback, useMemo, useRef, useState } from 'react';
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
  unmappedFolders?: UnmappedFolder[];
  items: ImportGameItem[];
  onInputChange: (ids: string[], name: string, value: unknown) => void;
  onImportPress: (ids: string[]) => void;
}

interface SelectedState {
  [key: string]: boolean;
}

function ImportGame(props: ImportGameProps) {
  const {
    rootFolderId,
    path,
    rootFoldersFetching,
    rootFoldersError,
    rootFoldersPopulated,
    unmappedFolders = [],
    items,
    onInputChange,
    onImportPress,
  } = props;

  const scrollerRef = useRef<HTMLDivElement | null>(null);

  const [allSelected, setAllSelected] = useState(false);
  const [allUnselected, setAllUnselected] = useState(false);
  const [lastToggled, setLastToggled] = useState<string | number | null>(null);
  const [selectedState, setSelectedState] = useState<SelectedState>({});

  const getSelectedIds = useCallback((): string[] => {
    return reduce(
      selectedState,
      (result: string[], value, id) => {
        if (value) {
          result.push(id);
        }

        return result;
      },
      []
    );
  }, [selectedState]);

  const selectedIds = useMemo(() => getSelectedIds(), [getSelectedIds]);

  const onSelectAllChange = useCallback(
    ({ value }: { value: boolean }) => {
      const result = selectAll(selectedState, value);
      setAllSelected(result.allSelected);
      setAllUnselected(result.allUnselected);
      setSelectedState(result.selectedState);
    },
    [selectedState]
  );

  const onSelectedChange = useCallback(
    ({ id, value, shiftKey }: SelectStateInputProps) => {
      const state = {
        allSelected,
        allUnselected,
        lastToggled,
        selectedState,
      };
      const result = toggleSelected(
        state,
        items,
        id,
        value ?? false,
        shiftKey ?? false
      );
      setAllSelected(result.allSelected);
      setAllUnselected(result.allUnselected);
      setLastToggled(result.lastToggled);
      setSelectedState(result.selectedState);
    },
    [allSelected, allUnselected, lastToggled, selectedState, items]
  );

  const onRemoveSelectedStateItem = useCallback((id: string) => {
    setSelectedState((prevState) => {
      const newState = { ...prevState };
      delete newState[id];
      return newState;
    });
  }, []);

  const handleInputChange = useCallback(
    ({ name, value }: { name: string; value: unknown }) => {
      onInputChange(selectedIds, name, value);
    },
    [selectedIds, onInputChange]
  );

  const handleImportPress = useCallback(() => {
    onImportPress(selectedIds);
  }, [selectedIds, onImportPress]);

  return (
    <PageContent title={translate('ImportGames')}>
      <PageContentBody ref={scrollerRef}>
        {rootFoldersFetching ? <LoadingIndicator /> : null}

        {!rootFoldersFetching && !!rootFoldersError ? (
          <Alert kind={kinds.DANGER}>{translate('RootFoldersLoadError')}</Alert>
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
        scrollerRef.current ? (
          <ImportGameTableConnector
            rootFolderId={rootFolderId}
            unmappedFolders={unmappedFolders}
            allSelected={allSelected}
            allUnselected={allUnselected}
            selectedState={selectedState}
            scroller={scrollerRef.current}
            onSelectAllChange={onSelectAllChange}
            onSelectedChange={onSelectedChange}
            onRemoveSelectedStateItem={onRemoveSelectedStateItem}
          />
        ) : null}
      </PageContentBody>

      {!rootFoldersError && !rootFoldersFetching && !!unmappedFolders.length ? (
        <ImportGameFooterConnector
          selectedIds={selectedIds}
          onInputChange={handleInputChange}
          onImportPress={handleImportPress}
        />
      ) : null}
    </PageContent>
  );
}

export default ImportGame;
