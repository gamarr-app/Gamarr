import { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { GAME_COMPONENT_SEARCH } from 'Commands/commandNames';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import { icons, kinds } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';

interface GameComponent {
  id: number;
  gameId: number;
  componentType: 'base' | 'update' | 'dlc';
  key: string;
  title: string;
  monitored: boolean;
  externalId: number;
  hasFile: boolean;
  sizeOnDisk: number;
}

const columns = [
  { name: 'type', label: () => translate('Type'), isVisible: true },
  { name: 'title', label: () => translate('Title'), isVisible: true },
  { name: 'size', label: () => translate('Size'), isVisible: true },
  { name: 'status', label: () => translate('Status'), isVisible: true },
  { name: 'monitored', label: '', isVisible: true },
  { name: 'actions', label: '', isVisible: true },
];

const typeKinds: Record<
  GameComponent['componentType'],
  'info' | 'success' | 'primary'
> = {
  base: 'info',
  update: 'success',
  dlc: 'primary',
};

interface GameComponentsTableProps {
  gameId: number;
}

function getStatusIcon(component: GameComponent) {
  if (component.hasFile) {
    return (
      <Icon
        name={icons.DOWNLOADED}
        kind={kinds.SUCCESS}
        title={translate('Downloaded')}
      />
    );
  }

  if (component.monitored) {
    return (
      <Icon
        name={icons.MISSING}
        kind={kinds.WARNING}
        title={translate('Missing')}
      />
    );
  }

  return (
    <Icon
      name={icons.UNMONITORED}
      kind={kinds.DISABLED}
      title={translate('Unmonitored')}
    />
  );
}

interface GameComponentRowProps {
  component: GameComponent;
  isSaving: boolean;
  onMonitorPress: (componentId: number, monitored: boolean) => void;
}

function GameComponentRow({
  component,
  isSaving,
  onMonitorPress,
}: GameComponentRowProps) {
  const dispatch = useDispatch();

  const isSearching = useSelector(
    useMemo(
      () =>
        createCommandExecutingSelector(GAME_COMPONENT_SEARCH, {
          componentId: component.id,
        }),
      [component.id]
    )
  );

  const handleMonitorPress = useCallback(
    (value: boolean) => {
      onMonitorPress(component.id, value);
    },
    [component.id, onMonitorPress]
  );

  const handleSearchPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: GAME_COMPONENT_SEARCH,
        gameId: component.gameId,
        componentId: component.id,
      })
    );
  }, [dispatch, component.gameId, component.id]);

  return (
    <TableRow>
      <TableRowCell>
        <Label kind={typeKinds[component.componentType]}>
          {component.componentType.toUpperCase()}
        </Label>
      </TableRowCell>

      <TableRowCell>{component.title}</TableRowCell>

      <TableRowCell>
        {component.hasFile ? formatBytes(component.sizeOnDisk) : '-'}
      </TableRowCell>

      <TableRowCell>{getStatusIcon(component)}</TableRowCell>

      <TableRowCell>
        <MonitorToggleButton
          monitored={component.monitored}
          isSaving={isSaving}
          onPress={handleMonitorPress}
        />
      </TableRowCell>

      <TableRowCell>
        <SpinnerIconButton
          name={icons.SEARCH}
          title={translate('Search')}
          isSpinning={isSearching}
          onPress={handleSearchPress}
        />
      </TableRowCell>
    </TableRow>
  );
}

function GameComponentsTable({ gameId }: GameComponentsTableProps) {
  const [components, setComponents] = useState<GameComponent[]>([]);
  const [isFetching, setIsFetching] = useState(true);
  const [savingIds, setSavingIds] = useState<Set<number>>(new Set());

  useEffect(() => {
    let aborted = false;
    setIsFetching(true);

    const { request } = createAjaxRequest({
      url: `/gamecomponent?gameId=${gameId}`,
    });

    request
      .then((data: GameComponent[]) => {
        if (!aborted) {
          setComponents(data);
        }
        return data;
      })
      .always(() => {
        if (!aborted) {
          setIsFetching(false);
        }
      });

    return () => {
      aborted = true;
    };
  }, [gameId]);

  const handleMonitorPress = useCallback(
    (componentId: number, monitored: boolean) => {
      setSavingIds((ids) => new Set(ids).add(componentId));

      const { request } = createAjaxRequest({
        url: `/gamecomponent/${componentId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify({ monitored }),
      });

      request
        .then((updated: GameComponent) => {
          setComponents((all) =>
            all.map((c) => (c.id === updated.id ? updated : c))
          );
          return updated;
        })
        .always(() => {
          setSavingIds((ids) => {
            const next = new Set(ids);
            next.delete(componentId);
            return next;
          });
        });
    },
    []
  );

  if (isFetching) {
    return <LoadingIndicator />;
  }

  if (!components.length) {
    return null;
  }

  return (
    <Table columns={columns}>
      <TableBody>
        {components.map((component) => (
          <GameComponentRow
            key={component.id}
            component={component}
            isSaving={savingIds.has(component.id)}
            onMonitorPress={handleMonitorPress}
          />
        ))}
      </TableBody>
    </Table>
  );
}

export default GameComponentsTable;
