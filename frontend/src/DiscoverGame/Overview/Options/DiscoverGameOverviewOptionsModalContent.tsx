import { useCallback } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

const posterSizeOptions = [
  {
    key: 'small',
    get value() {
      return translate('Small');
    },
  },
  {
    key: 'medium',
    get value() {
      return translate('Medium');
    },
  },
  {
    key: 'large',
    get value() {
      return translate('Large');
    },
  },
];

interface DiscoverGameOverviewOptionsModalContentProps {
  size: string;
  showYear: boolean;
  showStudio: boolean;
  showGenres: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  showCertification: boolean;
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
  onChangeOverviewOption: (payload: Record<string, unknown>) => void;
  onChangeOption: (payload: Record<string, unknown>) => void;
  onModalClose: (...args: unknown[]) => void;
}

function DiscoverGameOverviewOptionsModalContent(
  props: DiscoverGameOverviewOptionsModalContentProps
) {
  const {
    size,
    showYear,
    showStudio,
    showGenres,
    showIgdbRating,
    showMetacriticRating,
    showCertification,
    includeRecommendations,
    includeTrending,
    includePopular,
    onChangeOverviewOption,
    onChangeOption,
    onModalClose,
  } = props;

  const handleChangeOverviewOption = useCallback(
    ({ name, value }: { name: string; value: string | boolean }) => {
      onChangeOverviewOption({ [name]: value });
    },
    [onChangeOverviewOption]
  );

  const handleChangeOption = useCallback(
    ({ name, value }: { name: string; value: string | boolean }) => {
      onChangeOption({ [name]: value });
    },
    [onChangeOption]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('OverviewOptions')}</ModalHeader>

      <ModalBody>
        <Form>
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

          <FormGroup>
            <FormLabel>{translate('PosterSize')}</FormLabel>

            <FormInputGroup
              type={inputTypes.SELECT}
              name="size"
              value={size}
              values={posterSizeOptions}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowYear')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showYear"
              value={showYear}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowStudio')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showStudio"
              value={showStudio}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowGenres')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showGenres"
              value={showGenres}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowIgdbRating')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showIgdbRating"
              value={showIgdbRating}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowMetacriticRating')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showMetacriticRating"
              value={showMetacriticRating}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowCertification')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showCertification"
              value={showCertification}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>
        </Form>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default DiscoverGameOverviewOptionsModalContent;
