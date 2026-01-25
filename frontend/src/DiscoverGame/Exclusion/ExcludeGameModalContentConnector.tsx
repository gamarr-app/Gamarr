import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { addImportListExclusions } from 'Store/Actions/discoverGameActions';
import ExcludeGameModalContent from './ExcludeGameModalContent';

interface ExcludeGameModalContentConnectorProps {
  igdbId: number;
  title: string;
  year?: number;
  onModalClose: (didExclude?: boolean) => void;
}

function ExcludeGameModalContentConnector({
  igdbId,
  title,
  onModalClose,
}: ExcludeGameModalContentConnectorProps) {
  const dispatch = useDispatch();

  const handleExcludePress = useCallback(() => {
    dispatch(addImportListExclusions({ ids: [igdbId] }));
    onModalClose(true);
  }, [dispatch, igdbId, onModalClose]);

  return (
    <ExcludeGameModalContent
      igdbId={igdbId}
      title={title}
      onExcludePress={handleExcludePress}
      onModalClose={onModalClose}
    />
  );
}

export default ExcludeGameModalContentConnector;
