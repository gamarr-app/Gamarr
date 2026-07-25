import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';
import createGameIndexItemSelector from '../createGameIndexItemSelector';
import styles from './GameIndexPosterPlatformStrip.css';

// Keys are the serialized PlatformFamily enum values
const PLATFORM_LABELS: Record<string, string> = {
  pc: 'PC',
  playStation: 'PS',
  xbox: 'Xbox',
  nintendo: 'Nintendo',
  sega: 'Sega',
  atari: 'Atari',
  mobile: 'Mobile',
  linux: 'Linux',
  mac: 'Mac',
};

function platformLabel(platform: string | undefined): string {
  if (!platform || platform === 'unknown') {
    return translate('AnyPlatform');
  }

  return (
    PLATFORM_LABELS[platform] ??
    platform.charAt(0).toUpperCase() + platform.slice(1)
  );
}

interface PlatformChipProps {
  gameId: number;
}

function PlatformChip({ gameId }: PlatformChipProps) {
  const { game } = useSelector(
    useMemo(() => createGameIndexItemSelector(gameId), [gameId])
  );

  if (!game) {
    return null;
  }

  const { titleSlug, platform, hasFile, monitored } = game;

  let statusStyle = styles.unmonitored;
  let statusLabel = translate('Unmonitored');

  if (hasFile) {
    statusStyle = styles.downloaded;
    statusLabel = translate('Downloaded');
  } else if (monitored) {
    statusStyle = styles.missing;
    statusLabel = translate('Missing');
  }

  return (
    <Link
      className={styles.chip}
      to={`/game/${titleSlug}`}
      title={`${platformLabel(platform)}: ${statusLabel}`}
    >
      <span className={`${styles.status} ${statusStyle}`} />
      {platformLabel(platform)}
    </Link>
  );
}

interface GameIndexPosterPlatformStripProps {
  gameIds: number[];
}

// Compact per-platform status chips shown on a grouped poster (#150):
// one chip per library entry of the same title, linking to that entry.
function GameIndexPosterPlatformStrip({
  gameIds,
}: GameIndexPosterPlatformStripProps) {
  return (
    <div className={styles.strip}>
      {gameIds.map((gameId) => (
        <PlatformChip key={gameId} gameId={gameId} />
      ))}
    </div>
  );
}

export default GameIndexPosterPlatformStrip;
