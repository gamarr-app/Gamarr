import _ from 'lodash';
import PropTypes from 'prop-types';
import React from 'react';
import { icons } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import translate from 'Utilities/String/translate';
import DiscoverGameOverviewInfoRow from './DiscoverGameOverviewInfoRow';
import styles from './DiscoverGameOverviewInfo.css';

const infoRowHeight = parseInt(dimensions.gameIndexOverviewInfoRowHeight);

const rows = [
  {
    name: 'year',
    showProp: 'showYear',
    valueProp: 'year'
  },
  {
    name: 'studio',
    showProp: 'showStudio',
    valueProp: 'studio'
  },
  {
    name: 'genres',
    showProp: 'showGenres',
    valueProp: 'genres'
  },
  {
    name: 'igdbRating',
    showProp: 'showIgdbRating',
    valueProp: 'ratings.igdb.value'
  },
  {
    name: 'metacriticRating',
    showProp: 'showMetacriticRating',
    valueProp: 'ratings.metacritic.value'
  },
  {
    name: 'certification',
    showProp: 'showCertification',
    valueProp: 'certification'
  }
];

function isVisible(row, props) {
  const {
    name,
    showProp,
    valueProp
  } = row;

  return _.has(props, valueProp) && _.get(props, valueProp) !== null && (props[showProp] || props.sortKey === name);
}

function getInfoRowProps(row, props) {
  const { name } = row;

  if (name === 'year') {
    return {
      title: translate('Year'),
      iconName: icons.CALENDAR,
      label: props.year
    };
  }

  if (name === 'studio') {
    return {
      title: translate('Studio'),
      iconName: icons.STUDIO,
      label: props.studio
    };
  }

  if (name === 'genres') {
    return {
      title: translate('Genres'),
      iconName: icons.GENRE,
      label: props.genres.slice(0, 2).join(', ')
    };
  }

  if (name === 'igdbRating' && !!props.ratings.igdb) {
    return {
      title: translate('IgdbRating'),
      iconName: icons.HEART,
      label: `${(props.ratings.igdb.value * 10).toFixed()}%`
    };
  }

  if (name === 'metacriticRating' && !!props.ratings.metacritic) {
    return {
      title: translate('MetacriticRating'),
      iconName: icons.HEART,
      label: `${props.ratings.metacritic.value}`
    };
  }

  if (name === 'certification') {
    return {
      title: translate('Certification'),
      iconName: icons.FLAG,
      label: props.certification
    };
  }
}

function DiscoverGameOverviewInfo(props) {
  const {
    height
  } = props;

  let shownRows = 1;
  const maxRows = Math.floor(height / (infoRowHeight + 4));

  return (
    <div className={styles.infos}>
      {
        rows.map((row) => {
          if (!isVisible(row, props)) {
            return null;
          }

          if (shownRows >= maxRows) {
            return null;
          }

          shownRows++;

          const infoRowProps = getInfoRowProps(row, props);

          return (
            <DiscoverGameOverviewInfoRow
              key={row.name}
              {...infoRowProps}
            />
          );
        })
      }
    </div>
  );
}

DiscoverGameOverviewInfo.propTypes = {
  height: PropTypes.number.isRequired,
  showYear: PropTypes.bool.isRequired,
  showStudio: PropTypes.bool.isRequired,
  showGenres: PropTypes.bool.isRequired,
  showIgdbRating: PropTypes.bool.isRequired,
  showMetacriticRating: PropTypes.bool.isRequired,
  showCertification: PropTypes.bool.isRequired,
  studio: PropTypes.string,
  year: PropTypes.number,
  certification: PropTypes.string,
  ratings: PropTypes.object,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  sortKey: PropTypes.string.isRequired
};

export default DiscoverGameOverviewInfo;
