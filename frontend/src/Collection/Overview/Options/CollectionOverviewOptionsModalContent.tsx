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

interface PosterSizeOption {
  key: string;
  value: string;
}

const posterSizeOptions: PosterSizeOption[] = [
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

interface CollectionOverviewOptionsModalContentProps {
  detailedProgressBar: boolean;
  size: string;
  showDetails: boolean;
  showOverview: boolean;
  showPosters: boolean;
  onChangeOverviewOption: (payload: Record<string, unknown>) => void;
  onChangeOption: (payload: Record<string, unknown>) => void;
  onModalClose: () => void;
}

interface CollectionOverviewOptionsModalContentState {
  detailedProgressBar: boolean;
  size: string;
  showDetails: boolean;
  showOverview: boolean;
  showPosters: boolean;
}

class CollectionOverviewOptionsModalContent extends Component<
  CollectionOverviewOptionsModalContentProps,
  CollectionOverviewOptionsModalContentState
> {
  //
  // Lifecycle

  constructor(props: CollectionOverviewOptionsModalContentProps) {
    super(props);

    this.state = {
      detailedProgressBar: props.detailedProgressBar,
      size: props.size,
      showDetails: props.showDetails,
      showOverview: props.showOverview,
      showPosters: props.showPosters,
    };
  }

  componentDidUpdate(prevProps: CollectionOverviewOptionsModalContentProps) {
    const {
      detailedProgressBar,
      size,
      showDetails,
      showOverview,
      showPosters,
    } = this.props;

    const state: Partial<CollectionOverviewOptionsModalContentState> = {};

    if (detailedProgressBar !== prevProps.detailedProgressBar) {
      state.detailedProgressBar = detailedProgressBar;
    }

    if (size !== prevProps.size) {
      state.size = size;
    }

    if (showDetails !== prevProps.showDetails) {
      state.showDetails = showDetails;
    }

    if (showOverview !== prevProps.showOverview) {
      state.showOverview = showOverview;
    }

    if (showPosters !== prevProps.showPosters) {
      state.showPosters = showPosters;
    }

    if (!_.isEmpty(state)) {
      this.setState(state as CollectionOverviewOptionsModalContentState);
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
      } as Pick<
        CollectionOverviewOptionsModalContentState,
        keyof CollectionOverviewOptionsModalContentState
      >,
      () => {
        this.props.onChangeOverviewOption({ [name]: value });
      }
    );
  };

  onChangeOption = ({ name, value }: { name: string; value: unknown }) => {
    this.setState(
      {
        [name]: value,
      } as Pick<
        CollectionOverviewOptionsModalContentState,
        keyof CollectionOverviewOptionsModalContentState
      >,
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
      detailedProgressBar,
      showDetails,
      showPosters,
      showOverview,
    } = this.state;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>{translate('CollectionOptions')}</ModalHeader>

        <ModalBody>
          <Form>
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
              <FormLabel>{translate('DetailedProgressBar')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="detailedProgressBar"
                value={detailedProgressBar}
                helpText={translate('DetailedProgressBarHelpText')}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowCollectionDetails')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showDetails"
                value={showDetails}
                helpText={translate('CollectionShowDetailsHelpText')}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowOverview')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showOverview"
                value={showOverview}
                helpText={translate('CollectionShowOverviewsHelpText')}
                onChange={this.onChangeOverviewOption}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ShowPosters')}</FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="showPosters"
                value={showPosters}
                helpText={translate('CollectionShowPostersHelpText')}
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

export default CollectionOverviewOptionsModalContent;
