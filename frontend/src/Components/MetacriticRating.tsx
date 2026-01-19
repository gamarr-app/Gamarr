import React from 'react';
import Tooltip from 'Components/Tooltip/Tooltip';
import { Ratings, RatingValues } from 'Game/Game';
import { kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './MetacriticRating.css';

interface MetacriticRatingProps {
  ratings: Ratings;
  iconSize?: number;
  hideIcon?: boolean;
}

function MetacriticRating(props: MetacriticRatingProps) {
  const { ratings, iconSize = 14, hideIcon = false } = props;

  // Metacritic logo SVG as base64
  const metacriticImage =
    'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA1MCA1MCI+PGNpcmNsZSBjeD0iMjUiIGN5PSIyNSIgcj0iMjUiIGZpbGw9IiNmZmNjMzQiLz48dGV4dCB4PSIyNSIgeT0iMzIiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIyNCIgZm9udC13ZWlnaHQ9ImJvbGQiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZpbGw9IiMwMDAiPk08L3RleHQ+PC9zdmc+';

  const { metacritic: metacriticRatings = {} as RatingValues } = ratings;
  const { value = 0 } = metacriticRatings;

  return (
    <Tooltip
      anchor={
        <span className={styles.wrapper}>
          {!hideIcon && (
            <img
              className={styles.image}
              alt={translate('MetacriticRating')}
              src={metacriticImage}
              style={{
                height: `${iconSize}px`,
              }}
            />
          )}
          {value.toFixed()}
        </span>
      }
      tooltip={translate('MetacriticScore')}
      kind={kinds.INVERSE}
      position={tooltipPositions.TOP}
    />
  );
}

export default MetacriticRating;
