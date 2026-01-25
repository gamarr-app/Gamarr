import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function GameMinimumAvailabilityPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('Announced')}
        data={translate('AnnouncedGameAvailabilityDescription')}
      />

      <DescriptionListItem
        title={translate('InDevelopment')}
        data={translate('InDevelopmentGameAvailabilityDescription')}
      />

      <DescriptionListItem
        title={translate('Released')}
        data={translate('ReleasedGameAvailabilityDescription')}
      />
    </DescriptionList>
  );
}

export default GameMinimumAvailabilityPopoverContent;
