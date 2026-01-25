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

interface DiscoverGamePosterOptionsModalContentState {
  size: string;
  showTitle: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  includeRecommendations: boolean;
  includeTrending: boolean;
  includePopular: boolean;
}

class DiscoverGamePosterOptionsModalContent extends Component<
  DiscoverGamePosterOptionsModalContentProps,
  DiscoverGamePosterOptionsModalContentState
> {
  //
  // Lifecycle

  constructor(props: DiscoverGamePosterOptionsModalContentProps) {
    super(props);

    this.state = {
      size: props.size,
      showTitle: props.showTitle,
      showIgdbRating: props.showIgdbRating,
      showMetacriticRating: props.showMetacriticRating,
      includeRecommendations: props.includeRecommendations,
      includeTrending: props.includeTrending,
      includePopular: props.includePopular,
    };
  }

  componentDidUpdate(prevProps: DiscoverGamePosterOptionsModalContentProps) {
    const {
      size,
      showTitle,
      showIgdbRating,
      showMetacriticRating,
      includeRecommendations,
      includeTrending,
      includePopular,
    } = this.props;

    const state: Partial<DiscoverGamePosterOptionsModalContentState> = {};

    if (size !== prevProps.size) {
      state.size = size;
    }

    if (showTitle !== prevProps.showTitle) {
      state.showTitle = showTitle;
    }

    if (showIgdbRating !== prevProps.showIgdbRating) {
      state.showIgdbRating = showIgdbRating;
    }

    if (showMetacriticRating !== prevProps.showMetacriticRating) {
      state.showMetacriticRating = showMetacriticRating;
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
      this.setState(state as DiscoverGamePosterOptionsModalContentState);
    }
  }

  //
  // Listeners

  onChangePosterOption = ({
    name,
    value,
  }: {
    name: string;
    value: unknown;
  }) => {
    this.setState(
      {
        [name]: value,
      } as unknown as DiscoverGamePosterOptionsModalContentState,
      () => {
        this.props.onChangePosterOption({ [name]: value });
      }
    );
  };

  onChangeOption = ({ name, value }: { name: string; value: unknown }) => {
    this.setState(
      {
        [name]: value,
      } as unknown as DiscoverGamePosterOptionsModalContentState,
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
      showTitle,
      showIgdbRating,
      showMetacriticRating,
      includeRecommendations,
      includeTrending,
      includePopular,
    } = this.state;

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
                onChange={this.onChangePosterOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowTitle')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showTitle"
                value={showTitle}
                helpText={translate('ShowTitleHelpText')}
                onChange={this.onChangePosterOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowIgdbRating')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showIgdbRating"
                value={showIgdbRating}
                helpText={translate('ShowIgdbRatingHelpText')}
                onChange={this.onChangePosterOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowMetacriticRating')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showMetacriticRating"
                value={showMetacriticRating}
                helpText={translate('ShowMetacriticRatingHelpText')}
                onChange={this.onChangePosterOption}
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

export default DiscoverGamePosterOptionsModalContent;
