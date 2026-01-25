import { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds } from 'Helpers/Props';
import {
  clearGameHistory,
  fetchGameHistory,
  gameHistoryMarkAsFailed,
} from 'Store/Actions/gameHistoryActions';
import translate from 'Utilities/String/translate';
import GameHistoryRow from './GameHistoryRow';

const columns: Column[] = [
  {
    name: 'eventType',
    label: '',
    isVisible: true,
  },
  {
    name: 'sourceTitle',
    label: () => translate('SourceTitle'),
    isVisible: true,
  },
  {
    name: 'languages',
    label: () => translate('Languages'),
    isVisible: true,
  },
  {
    name: 'quality',
    label: () => translate('Quality'),
    isVisible: true,
  },
  {
    name: 'customFormats',
    label: () => translate('CustomFormats'),
    isSortable: false,
    isVisible: true,
  },
  {
    name: 'customFormatScore',
    label: React.createElement(Icon, {
      name: icons.SCORE,
      title: () => translate('CustomFormatScore'),
    }),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'date',
    label: () => translate('Date'),
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

export interface GameHistoryModalContentProps {
  gameId: number;
  onModalClose: () => void;
}

function GameHistoryModalContent({
  gameId,
  onModalClose,
}: GameHistoryModalContentProps) {
  const dispatch = useDispatch();

  const { isFetching, isPopulated, error, items } = useSelector(
    (state: AppState) => state.gameHistory
  );

  const hasItems = !!items.length;

  const handleMarkAsFailedPress = useCallback(
    (historyId: number) => {
      dispatch(
        gameHistoryMarkAsFailed({
          historyId,
          gameId,
        })
      );
    },
    [gameId, dispatch]
  );

  useEffect(() => {
    dispatch(fetchGameHistory({ gameId }));

    return () => {
      dispatch(clearGameHistory());
    };
  }, [gameId, dispatch]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('History')}</ModalHeader>

      <ModalBody>
        {isFetching && !isPopulated ? <LoadingIndicator /> : null}

        {!isFetching && !!error ? (
          <Alert kind={kinds.DANGER}>{translate('HistoryLoadError')}</Alert>
        ) : null}

        {isPopulated && !hasItems && !error ? (
          <div>{translate('NoHistory')}</div>
        ) : null}

        {isPopulated && hasItems && !error && (
          <Table columns={columns}>
            <TableBody>
              {items.map((item) => {
                return (
                  <GameHistoryRow
                    key={item.id}
                    {...item}
                    onMarkAsFailedPress={handleMarkAsFailedPress}
                  />
                );
              })}
            </TableBody>
          </Table>
        )}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default GameHistoryModalContent;
