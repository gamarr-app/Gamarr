import _ from 'lodash';
import { IconName } from 'Components/Icon';
import { Ratings } from 'Game/Game';
import { icons } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import translate from 'Utilities/String/translate';
import DiscoverGameOverviewInfoRow from './DiscoverGameOverviewInfoRow';
import styles from './DiscoverGameOverviewInfo.css';

const infoRowHeight = parseInt(dimensions.gameIndexOverviewInfoRowHeight);

interface RowConfig {
  name: string;
  showProp: string;
  valueProp: string;
}

interface RowInfoProps {
  title: string;
  iconName: IconName;
  label: string | null;
}

interface DiscoverGameOverviewInfoProps {
  height: number;
  showYear: boolean;
  showStudio: boolean;
  showGenres: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  showCertification: boolean;
  studio?: string;
  year?: number;
  certification?: string;
  ratings?: Ratings;
  genres: string[];
  sortKey: string;
}

const rows: RowConfig[] = [
  {
    name: 'year',
    showProp: 'showYear',
    valueProp: 'year',
  },
  {
    name: 'studio',
    showProp: 'showStudio',
    valueProp: 'studio',
  },
  {
    name: 'genres',
    showProp: 'showGenres',
    valueProp: 'genres',
  },
  {
    name: 'igdbRating',
    showProp: 'showIgdbRating',
    valueProp: 'ratings.igdb.value',
  },
  {
    name: 'metacriticRating',
    showProp: 'showMetacriticRating',
    valueProp: 'ratings.metacritic.value',
  },
  {
    name: 'certification',
    showProp: 'showCertification',
    valueProp: 'certification',
  },
];

type ShowProps = Pick<
  DiscoverGameOverviewInfoProps,
  | 'showYear'
  | 'showStudio'
  | 'showGenres'
  | 'showIgdbRating'
  | 'showMetacriticRating'
  | 'showCertification'
>;

function isVisible(row: RowConfig, props: DiscoverGameOverviewInfoProps) {
  const { name, showProp, valueProp } = row;

  return (
    _.has(props, valueProp) &&
    _.get(props, valueProp) !== null &&
    (props[showProp as keyof ShowProps] || props.sortKey === name)
  );
}

function getInfoRowProps(
  row: RowConfig,
  props: DiscoverGameOverviewInfoProps
): RowInfoProps | undefined {
  const { name } = row;

  if (name === 'year') {
    return {
      title: translate('Year'),
      iconName: icons.CALENDAR,
      label: props.year?.toString() ?? null,
    };
  }

  if (name === 'studio') {
    return {
      title: translate('Studio'),
      iconName: icons.STUDIO,
      label: props.studio ?? null,
    };
  }

  if (name === 'genres') {
    return {
      title: translate('Genres'),
      iconName: icons.GENRE,
      label: props.genres.slice(0, 2).join(', '),
    };
  }

  if (name === 'igdbRating' && !!props.ratings?.igdb) {
    return {
      title: translate('IgdbRating'),
      iconName: icons.HEART,
      label: `${(props.ratings.igdb.value * 10).toFixed()}%`,
    };
  }

  if (name === 'metacriticRating' && !!props.ratings?.metacritic) {
    return {
      title: translate('MetacriticRating'),
      iconName: icons.HEART,
      label: `${props.ratings.metacritic.value}`,
    };
  }

  if (name === 'certification') {
    return {
      title: translate('Certification'),
      iconName: icons.FLAG,
      label: props.certification ?? null,
    };
  }

  return undefined;
}

function DiscoverGameOverviewInfo(props: DiscoverGameOverviewInfoProps) {
  const { height } = props;

  let shownRows = 1;
  const maxRows = Math.floor(height / (infoRowHeight + 4));

  return (
    <div className={styles.infos}>
      {rows.map((row) => {
        if (!isVisible(row, props)) {
          return null;
        }

        if (shownRows >= maxRows) {
          return null;
        }

        shownRows++;

        const infoRowProps = getInfoRowProps(row, props);

        if (!infoRowProps) {
          return null;
        }

        return <DiscoverGameOverviewInfoRow key={row.name} {...infoRowProps} />;
      })}
    </div>
  );
}

export default DiscoverGameOverviewInfo;
