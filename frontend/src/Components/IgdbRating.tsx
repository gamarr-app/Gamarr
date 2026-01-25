import Tooltip from 'Components/Tooltip/Tooltip';
import { Ratings } from 'Game/Game';
import { kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './IgdbRating.css';

interface IgdbRatingProps {
  ratings: Ratings;
  iconSize?: number;
  hideIcon?: boolean;
}

function IgdbRating(props: IgdbRatingProps) {
  const { ratings, iconSize = 14, hideIcon = false } = props;

  // Handle case where ratings object is undefined or has no value
  if (!ratings || !ratings.igdb || !ratings.igdb.value) {
    return null;
  }

  // IGDB logo - purple "IGDB" text
  const igdbImage =
    'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4MCAzMCI+PHJlY3Qgd2lkdGg9IjgwIiBoZWlnaHQ9IjMwIiByeD0iNCIgZmlsbD0iIzlhNDdmZiIvPjx0ZXh0IHg9IjQwIiB5PSIyMSIgZm9udC1mYW1pbHk9IkFyaWFsLCBzYW5zLXNlcmlmIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iYm9sZCIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZmlsbD0iI2ZmZiI+SUdEQjwvdGV4dD48L3N2Zz4=';

  const { value = 0, votes = 0 } = ratings.igdb;

  return (
    <Tooltip
      anchor={
        <span className={styles.wrapper}>
          {!hideIcon && (
            <img
              className={styles.image}
              alt={translate('IgdbRating')}
              src={igdbImage}
              style={{
                height: `${iconSize}px`,
              }}
            />
          )}
          {value.toFixed()}%
        </span>
      }
      tooltip={translate('CountVotes', { votes })}
      kind={kinds.INVERSE}
      position={tooltipPositions.TOP}
    />
  );
}

export default IgdbRating;
