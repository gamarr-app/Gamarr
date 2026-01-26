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

interface DiscoverGamePosterOptionsModalContentProps {
  size: string;
  showTitle: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
  onChangePosterOption: (payload: Record<string, unknown>) => void;
  onChangeOption: (payload: Record<string, unknown>) => void;
  onModalClose: (...args: unknown[]) => void;
}

function DiscoverGamePosterOptionsModalContent(
  props: DiscoverGamePosterOptionsModalContentProps
) {
  const {
    size,
    showTitle,
    showIgdbRating,
    showMetacriticRating,
    includeRecommendations,
    includeTrending,
    includePopular,
    onChangePosterOption,
    onChangeOption,
    onModalClose,
  } = props;

  const handleChangePosterOption = useCallback(
    ({ name, value }: { name: string; value: string | boolean }) => {
      onChangePosterOption({ [name]: value });
    },
    [onChangePosterOption]
  );

  const handleChangeOption = useCallback(
    ({ name, value }: { name: string; value: string | boolean }) => {
      onChangeOption({ [name]: value });
    },
    [onChangeOption]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('PosterOptions')}</ModalHeader>

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
              onChange={handleChangePosterOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowTitle')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showTitle"
              value={showTitle}
              helpText={translate('ShowTitleHelpText')}
              onChange={handleChangePosterOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowIgdbRating')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showIgdbRating"
              value={showIgdbRating}
              helpText={translate('ShowIgdbRatingHelpText')}
              onChange={handleChangePosterOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowMetacriticRating')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showMetacriticRating"
              value={showMetacriticRating}
              helpText={translate('ShowMetacriticRatingHelpText')}
              onChange={handleChangePosterOption}
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

export default DiscoverGamePosterOptionsModalContent;
