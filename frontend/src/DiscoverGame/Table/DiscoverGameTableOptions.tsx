import { useCallback, useEffect, useState } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';

interface DiscoverGameTableOptionsProps {
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
  onChangeOption: (options: Partial<DiscoverGameOptions>) => void;
}

interface DiscoverGameOptions {
  includeRecommendations?: boolean;
  includeTrending?: boolean;
  includePopular?: boolean;
}

function DiscoverGameTableOptions({
  includeRecommendations: initialIncludeRecommendations,
  includeTrending: initialIncludeTrending,
  includePopular: initialIncludePopular,
  onChangeOption,
}: DiscoverGameTableOptionsProps) {
  const [includeRecommendations, setIncludeRecommendations] = useState(
    initialIncludeRecommendations
  );
  const [includeTrending, setIncludeTrending] = useState(
    initialIncludeTrending
  );
  const [includePopular, setIncludePopular] = useState(initialIncludePopular);

  useEffect(() => {
    setIncludeRecommendations(initialIncludeRecommendations);
  }, [initialIncludeRecommendations]);

  useEffect(() => {
    setIncludeTrending(initialIncludeTrending);
  }, [initialIncludeTrending]);

  useEffect(() => {
    setIncludePopular(initialIncludePopular);
  }, [initialIncludePopular]);

  const handleChangeOption = useCallback(
    ({ name, value }: InputChanged<boolean>) => {
      switch (name) {
        case 'includeRecommendations':
          setIncludeRecommendations(value);
          break;
        case 'includeTrending':
          setIncludeTrending(value);
          break;
        case 'includePopular':
          setIncludePopular(value);
          break;
        default:
          break;
      }

      onChangeOption({ [name]: value });
    },
    [onChangeOption]
  );

  return (
    <>
      <FormGroup>
        <FormLabel>{translate('IncludeGamarrRecommendations')}</FormLabel>

        <FormInputGroup
          type={inputTypes.CHECK}
          name="includeRecommendations"
          value={includeRecommendations}
          helpText={translate('IncludeRecommendationsHelpText')}
          onChange={handleChangeOption}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('IncludeTrending')}</FormLabel>

        <FormInputGroup
          type={inputTypes.CHECK}
          name="includeTrending"
          value={includeTrending}
          helpText={translate('IncludeTrendingGamesHelpText')}
          onChange={handleChangeOption}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('IncludePopular')}</FormLabel>

        <FormInputGroup
          type={inputTypes.CHECK}
          name="includePopular"
          value={includePopular}
          helpText={translate('IncludePopularGamesHelpText')}
          onChange={handleChangeOption}
        />
      </FormGroup>
    </>
  );
}

export default DiscoverGameTableOptions;
