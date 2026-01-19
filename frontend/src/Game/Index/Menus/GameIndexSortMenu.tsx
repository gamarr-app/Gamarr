import React from 'react';
import MenuContent from 'Components/Menu/MenuContent';
import SortMenu from 'Components/Menu/SortMenu';
import SortMenuItem from 'Components/Menu/SortMenuItem';
import { align } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import translate from 'Utilities/String/translate';

interface GameIndexSortMenuProps {
  sortKey?: string;
  sortDirection?: SortDirection;
  isDisabled: boolean;
  onSortSelect(sortKey: string): void;
}

function GameIndexSortMenu(props: GameIndexSortMenuProps) {
  const { sortKey, sortDirection, isDisabled, onSortSelect } = props;

  return (
    <SortMenu isDisabled={isDisabled} alignMenu={align.RIGHT}>
      <MenuContent>
        <SortMenuItem
          name="status"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('MonitoredStatus')}
        </SortMenuItem>

        <SortMenuItem
          name="sortTitle"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Title')}
        </SortMenuItem>

        <SortMenuItem
          name="studio"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Studio')}
        </SortMenuItem>

        <SortMenuItem
          name="qualityProfileId"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('QualityProfile')}
        </SortMenuItem>

        <SortMenuItem
          name="added"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Added')}
        </SortMenuItem>

        <SortMenuItem
          name="year"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Year')}
        </SortMenuItem>

        <SortMenuItem
          name="inCinemas"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('InDevelopment')}
        </SortMenuItem>

        <SortMenuItem
          name="digitalRelease"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('DigitalRelease')}
        </SortMenuItem>

        <SortMenuItem
          name="physicalRelease"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('PhysicalRelease')}
        </SortMenuItem>

        <SortMenuItem
          name="releaseDate"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('ReleaseDate')}
        </SortMenuItem>

        <SortMenuItem
          name="igdbRating"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('IgdbRating')}
        </SortMenuItem>

        <SortMenuItem
          name="metacriticRating"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('MetacriticRating')}
        </SortMenuItem>

        <SortMenuItem
          name="popularity"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Popularity')}
        </SortMenuItem>

        <SortMenuItem
          name="path"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Path')}
        </SortMenuItem>

        <SortMenuItem
          name="sizeOnDisk"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('SizeOnDisk')}
        </SortMenuItem>

        <SortMenuItem
          name="certification"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Certification')}
        </SortMenuItem>

        <SortMenuItem
          name="originalTitle"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('OriginalTitle')}
        </SortMenuItem>

        <SortMenuItem
          name="originalLanguage"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('OriginalLanguage')}
        </SortMenuItem>

        <SortMenuItem
          name="tags"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Tags')}
        </SortMenuItem>
      </MenuContent>
    </SortMenu>
  );
}

export default GameIndexSortMenu;
