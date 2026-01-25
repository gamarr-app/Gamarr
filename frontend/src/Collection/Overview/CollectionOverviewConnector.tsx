import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { GameStatus, Image } from 'Game/Game';
import { toggleCollectionMonitored } from 'Store/Actions/gameCollectionActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import { SelectStateInputProps } from 'typings/props';
import CollectionOverview from './CollectionOverview';

interface OverviewOptions {
  showDetails: boolean;
  showOverview: boolean;
  showPosters: boolean;
  detailedProgressBar: boolean;
}

interface CollectionGame {
  igdbId: number;
  title: string;
  year: number;
  status: GameStatus;
  overview?: string;
  images: Image[];
  [key: string]: unknown;
}

interface CollectionOverviewConnectorProps {
  collectionId: number;
  id: number;
  monitored: boolean;
  qualityProfileId: number;
  minimumAvailability: string;
  searchOnAdd: boolean;
  rootFolderPath: string;
  igdbId: number;
  title: string;
  overview: string;
  games: CollectionGame[];
  genres: string[];
  missingGames: number;
  images: Image[];
  rowHeight: number;
  posterHeight: number;
  posterWidth: number;
  overviewOptions: OverviewOptions;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  timeFormat: string;
  isSelected?: boolean;
  onSelectedChange: (props: SelectStateInputProps) => void;
}

function CollectionOverviewConnector(props: CollectionOverviewConnectorProps) {
  const dispatch = useDispatch();
  const { isSmallScreen } = useSelector(createDimensionsSelector());

  const onMonitorTogglePress = useCallback(
    (monitored: boolean) => {
      dispatch(
        toggleCollectionMonitored({
          collectionId: props.collectionId,
          monitored,
        })
      );
    },
    [dispatch, props.collectionId]
  );

  return (
    <CollectionOverview
      {...props}
      isSmallScreen={isSmallScreen}
      onMonitorTogglePress={onMonitorTogglePress}
    />
  );
}

export default CollectionOverviewConnector;
