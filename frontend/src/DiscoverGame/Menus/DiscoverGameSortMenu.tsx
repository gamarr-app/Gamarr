import React from 'react';
import MenuContent from 'Components/Menu/MenuContent';
import SortMenu from 'Components/Menu/SortMenu';
import SortMenuItem from 'Components/Menu/SortMenuItem';
import { align } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import translate from 'Utilities/String/translate';

interface DiscoverGameSortMenuProps {
  sortKey?: string;
  sortDirection?: SortDirection;
  isDisabled: boolean;
  onSortSelect: (sortKey: string) => void;
}

function DiscoverGameSortMenu({
  sortKey,
  sortDirection,
  isDisabled,
  onSortSelect,
}: DiscoverGameSortMenuProps) {
  return (
    <SortMenu isDisabled={isDisabled} alignMenu={align.RIGHT}>
      <MenuContent>
        <SortMenuItem
          name="status"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Status')}
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
          name="runtime"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Runtime')}
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
          name="certification"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Certification')}
        </SortMenuItem>
      </MenuContent>
    </SortMenu>
  );
}

export default DiscoverGameSortMenu;
