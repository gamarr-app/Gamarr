import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import { GAME_COMPONENT_SEARCH } from 'Commands/commandNames';
import EnhancedSelectInput from 'Components/Form/Select/EnhancedSelectInput';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
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
import GameInteractiveSearchModal from '../../Search/GameInteractiveSearchModal';

interface GameComponent {
  id: number;
  gameId: number;
  componentType:
    | 'base'
    | 'update'
    | 'dlc'
    | 'noIntroRetailRom'
    | 'noIntroMultiboot'
    | 'noIntroVideo'
    | 'noIntroBios'
    | 'noIntroRomhackOrUnverified';
  key: string;
  title: string;
  monitored: boolean;
  externalId: number;
  qualityProfileId: number;
  hasFile: boolean;
  sizeOnDisk: number;
  noIntroCatalogMatches: NoIntroCatalogMatch[];
}

interface NoIntroCatalogMatch {
  id: number;
  catalogSourceId: number;
  sourceName?: string;
  systemKey: string;
  canonicalName: string;
  canonicalFileName: string;
  catalogVersion?: string;
  lastSyncError?: string;
  hashType?: string;
  hashValue?: string;
}

const columns = [
  { name: 'type', label: () => translate('Type'), isVisible: true },
  { name: 'title', label: () => translate('Title'), isVisible: true },
  { name: 'size', label: () => translate('Size'), isVisible: true },
  { name: 'status', label: () => translate('Status'), isVisible: true },
  { name: 'noIntro', label: () => 'No-Intro', isVisible: true },
  {
    name: 'qualityProfileId',
    label: () => translate('QualityProfile'),
    isVisible: true,
  },
  { name: 'monitored', label: '', isVisible: true },
  { name: 'actions', label: '', isVisible: true },
];

const typeKinds: Record<
  GameComponent['componentType'],
  'info' | 'success' | 'primary' | 'warning' | 'danger'
> = {
  base: 'info',
  update: 'success',
  dlc: 'primary',
  noIntroRetailRom: 'info',
  noIntroMultiboot: 'success',
  noIntroVideo: 'primary',
  noIntroBios: 'warning',
  noIntroRomhackOrUnverified: 'danger',
};

const typeLabels: Record<GameComponent['componentType'], string> = {
  base: 'BASE',
  update: 'UPDATE',
  dlc: 'DLC',
  noIntroRetailRom: 'ROM',
  noIntroMultiboot: 'MULTIBOOT',
  noIntroVideo: 'VIDEO',
  noIntroBios: 'BIOS',
  noIntroRomhackOrUnverified: 'UNVERIFIED',
};

const noIntroSystemNames: Record<string, string> = {
  'nintendo---game-boy': 'Nintendo Game Boy',
  'nintendo---game-boy-color': 'Nintendo Game Boy Color',
  'nintendo---game-boy-advance': 'Nintendo Game Boy Advance',
  'nintendo---game-boy-advance-multiboot': 'Nintendo GBA Multiboot',
  'nintendo---game-boy-advance--multiboot': 'Nintendo GBA Multiboot',
  'nintendo---game-boy-advance-e-reader': 'Nintendo GBA e-Reader',
  'nintendo---game-boy-advance--e-reader': 'Nintendo GBA e-Reader',
  'nintendo---game-boy-advance-play-yan': 'Nintendo GBA Play-Yan',
  'nintendo---game-boy-advance--play-yan': 'Nintendo GBA Play-Yan',
  'nintendo---game-boy-advance-video': 'Nintendo GBA Video',
  'nintendo---game-boy-advance--video': 'Nintendo GBA Video',
  'nintendo---nintendo-ds': 'Nintendo DS',
  'nintendo---nintendo-ds-download-play': 'Nintendo DS Download Play',
  'nintendo---nintendo-ds--download-play': 'Nintendo DS Download Play',
  'nintendo---nintendo-ds-dsvision-sd-cards': 'Nintendo DS DSvision',
  'nintendo---nintendo-ds--dsvision-sd-cards': 'Nintendo DS DSvision',
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

function getNoIntroStatus(component: GameComponent) {
  const matches = component.noIntroCatalogMatches ?? [];

  if (matches.length === 0) {
    return '-';
  }

  const match = matches[0];

  if (match == null) {
    return '-';
  }

  const suffix = matches.length > 1 ? ` +${matches.length - 1}` : '';
  const version = match.catalogVersion ? ` (${match.catalogVersion})` : '';
  const systemName = noIntroSystemNames[match.systemKey] ?? match.systemKey;
  const title = [
    `${match.sourceName ?? 'No-Intro'}${version}`,
    ...matches.flatMap((catalogMatch) => {
      const lines = [catalogMatch.canonicalFileName];

      if (catalogMatch.hashType && catalogMatch.hashValue) {
        lines.push(
          `${catalogMatch.hashType.toUpperCase()} ${catalogMatch.hashValue}`
        );
      }

      return lines;
    }),
  ].join('\n');

  return (
    <Label
      kind={match.lastSyncError ? kinds.WARNING : kinds.SUCCESS}
      title={title}
    >
      {`${systemName}${suffix}`}
    </Label>
  );
}

interface GameComponentRowProps {
  component: GameComponent;
  isSaving: boolean;
  profileValues: { key: number; value: string }[];
  onMonitorPress: (componentId: number, monitored: boolean) => void;
  onProfileChange: (componentId: number, qualityProfileId: number) => void;
}

function GameComponentRow({
  component,
  isSaving,
  profileValues,
  onMonitorPress,
  onProfileChange,
}: GameComponentRowProps) {
  const dispatch = useDispatch();
  const [isInteractiveSearchModalOpen, setIsInteractiveSearchModalOpen] =
    useState(false);

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

  const handleInteractiveSearchPress = useCallback(() => {
    setIsInteractiveSearchModalOpen(true);
  }, []);

  const handleInteractiveSearchModalClose = useCallback(() => {
    setIsInteractiveSearchModalOpen(false);
  }, []);

  const handleProfileChange = useCallback(
    ({ value }: { value: number }) => {
      onProfileChange(component.id, value);
    },
    [component.id, onProfileChange]
  );

  return (
    <TableRow>
      <TableRowCell>
        <Label kind={typeKinds[component.componentType]}>
          {typeLabels[component.componentType]}
        </Label>
      </TableRowCell>

      <TableRowCell>{component.title}</TableRowCell>

      <TableRowCell>
        {component.hasFile ? formatBytes(component.sizeOnDisk) : '-'}
      </TableRowCell>

      <TableRowCell>{getStatusIcon(component)}</TableRowCell>

      <TableRowCell>{getNoIntroStatus(component)}</TableRowCell>

      <TableRowCell>
        {component.componentType === 'dlc' ? (
          <EnhancedSelectInput
            name={`componentProfile-${component.id}`}
            value={component.qualityProfileId}
            values={profileValues}
            onChange={handleProfileChange}
          />
        ) : (
          '-'
        )}
      </TableRowCell>

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
          title={translate('AutomaticSearch')}
          isSpinning={isSearching}
          onPress={handleSearchPress}
        />

        <IconButton
          name={icons.INTERACTIVE}
          title={translate('InteractiveSearch')}
          onPress={handleInteractiveSearchPress}
        />

        <GameInteractiveSearchModal
          isOpen={isInteractiveSearchModalOpen}
          gameId={component.gameId}
          componentId={component.id}
          componentTitle={component.title}
          onModalClose={handleInteractiveSearchModalClose}
        />
      </TableRowCell>
    </TableRow>
  );
}

function GameComponentsTable({ gameId }: GameComponentsTableProps) {
  const [components, setComponents] = useState<GameComponent[]>([]);
  const [isFetching, setIsFetching] = useState(true);
  const [savingIds, setSavingIds] = useState<Set<number>>(new Set());

  const qualityProfiles = useSelector(
    (state: AppState) => state.settings.qualityProfiles.items
  );

  const profileValues = useMemo(() => {
    return [{ key: 0, value: translate('InheritFromGame') }].concat(
      qualityProfiles.map((p) => ({ key: p.id, value: p.name }))
    );
  }, [qualityProfiles]);

  const visibleComponents = useMemo(() => {
    const hasNoIntroVariants = components.some((component) =>
      component.componentType.startsWith('noIntro')
    );

    if (!hasNoIntroVariants) {
      return components;
    }

    return components.filter((component) => component.componentType !== 'base');
  }, [components]);

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

  const componentsRef = useRef(components);
  componentsRef.current = components;

  // The PUT replaces both fields, so always send the component's full
  // current state — a partial body would reset the omitted field.
  const saveComponent = useCallback(
    (componentId: number, changes: Partial<GameComponent>) => {
      const current = componentsRef.current.find((c) => c.id === componentId);

      if (!current) {
        return;
      }

      setSavingIds((ids) => new Set(ids).add(componentId));

      const { request } = createAjaxRequest({
        url: `/gamecomponent/${componentId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify({
          monitored: current.monitored,
          qualityProfileId: current.qualityProfileId,
          ...changes,
        }),
      });

      request
        .then((updated: GameComponent) => {
          setComponents((latest) =>
            latest.map((c) => (c.id === updated.id ? updated : c))
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

  const handleMonitorPress = useCallback(
    (componentId: number, monitored: boolean) => {
      saveComponent(componentId, { monitored });
    },
    [saveComponent]
  );

  const handleProfileChange = useCallback(
    (componentId: number, qualityProfileId: number) => {
      saveComponent(componentId, { qualityProfileId });
    },
    [saveComponent]
  );

  if (isFetching) {
    return <LoadingIndicator />;
  }

  if (!visibleComponents.length) {
    return null;
  }

  return (
    <Table columns={columns}>
      <TableBody>
        {visibleComponents.map((component) => (
          <GameComponentRow
            key={component.id}
            component={component}
            isSaving={savingIds.has(component.id)}
            profileValues={profileValues}
            onMonitorPress={handleMonitorPress}
            onProfileChange={handleProfileChange}
          />
        ))}
      </TableBody>
    </Table>
  );
}

export default GameComponentsTable;
