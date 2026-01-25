import _ from 'lodash';
import { Component } from 'react';
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

interface DiscoverGameOverviewOptionsModalContentState {
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
}

class DiscoverGameOverviewOptionsModalContent extends Component<
  DiscoverGameOverviewOptionsModalContentProps,
  DiscoverGameOverviewOptionsModalContentState
> {
  //
  // Lifecycle

  constructor(props: DiscoverGameOverviewOptionsModalContentProps) {
    super(props);

    this.state = {
      size: props.size,
      showYear: props.showYear,
      showStudio: props.showStudio,
      showGenres: props.showGenres,
      showIgdbRating: props.showIgdbRating,
      showMetacriticRating: props.showMetacriticRating,
      showCertification: props.showCertification,
      includeRecommendations: props.includeRecommendations,
      includeTrending: props.includeTrending,
      includePopular: props.includePopular,
    };
  }

  componentDidUpdate(prevProps: DiscoverGameOverviewOptionsModalContentProps) {
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
    } = this.props;

    const state: Partial<DiscoverGameOverviewOptionsModalContentState> = {};

    if (size !== prevProps.size) {
      state.size = size;
    }

    if (showYear !== prevProps.showYear) {
      state.showYear = showYear;
    }

    if (showStudio !== prevProps.showStudio) {
      state.showStudio = showStudio;
    }

    if (showGenres !== prevProps.showGenres) {
      state.showGenres = showGenres;
    }

    if (showIgdbRating !== prevProps.showIgdbRating) {
      state.showIgdbRating = showIgdbRating;
    }

    if (showMetacriticRating !== prevProps.showMetacriticRating) {
      state.showMetacriticRating = showMetacriticRating;
    }

    if (showCertification !== prevProps.showCertification) {
      state.showCertification = showCertification;
    }

    if (includeRecommendations !== prevProps.includeRecommendations) {
      state.includeRecommendations = includeRecommendations;
    }

    if (includeTrending !== prevProps.includeTrending) {
      state.includeTrending = includeTrending;
    }

    if (includePopular !== prevProps.includePopular) {
      state.includePopular = includePopular;
    }

    if (!_.isEmpty(state)) {
      this.setState(state as DiscoverGameOverviewOptionsModalContentState);
    }
  }

  //
  // Listeners

  onChangeOverviewOption = ({
    name,
    value,
  }: {
    name: string;
    value: unknown;
  }) => {
    this.setState(
      {
        [name]: value,
      } as unknown as DiscoverGameOverviewOptionsModalContentState,
      () => {
        this.props.onChangeOverviewOption({ [name]: value });
      }
    );
  };

  onChangeOption = ({ name, value }: { name: string; value: unknown }) => {
    this.setState(
      {
        [name]: value,
      } as unknown as DiscoverGameOverviewOptionsModalContentState,
      () => {
        this.props.onChangeOption({
          [name]: value,
        });
      }
    );
  };

  //
  // Render

  render() {
    const { onModalClose } = this.props;

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
    } = this.state;

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
                onChange={this.onChangeOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('IncludeTrending')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="includeTrending"
                value={includeTrending}
                helpText={translate('IncludeTrendingGamesHelpText')}
                onChange={this.onChangeOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('IncludePopular')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="includePopular"
                value={includePopular}
                helpText={translate('IncludePopularGamesHelpText')}
                onChange={this.onChangeOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('PosterSize')}</FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="size"
                value={size}
                values={posterSizeOptions}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowYear')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showYear"
                value={showYear}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowStudio')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showStudio"
                value={showStudio}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowGenres')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showGenres"
                value={showGenres}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowIgdbRating')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showIgdbRating"
                value={showIgdbRating}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowMetacriticRating')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showMetacriticRating"
                value={showMetacriticRating}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowCertification')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showCertification"
                value={showCertification}
                onChange={this.onChangeOverviewOption}
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
}

export default DiscoverGameOverviewOptionsModalContent;
