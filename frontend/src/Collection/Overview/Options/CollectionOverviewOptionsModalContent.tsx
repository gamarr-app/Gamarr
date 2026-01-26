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

function CollectionOverviewOptionsModalContent(
  props: CollectionOverviewOptionsModalContentProps
) {
  const {
    detailedProgressBar,
    size,
    showDetails,
    showOverview,
    showPosters,
    onChangeOverviewOption,
    onModalClose,
  } = props;

  const handleChangeOverviewOption = useCallback(
    ({ name, value }: { name: string; value: unknown }) => {
      onChangeOverviewOption({ [name]: value });
    },
    [onChangeOverviewOption]
  );

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
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('DetailedProgressBar')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="detailedProgressBar"
              value={detailedProgressBar}
              helpText={translate('DetailedProgressBarHelpText')}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowCollectionDetails')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showDetails"
              value={showDetails}
              helpText={translate('CollectionShowDetailsHelpText')}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowOverview')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showOverview"
              value={showOverview}
              helpText={translate('CollectionShowOverviewsHelpText')}
              onChange={handleChangeOverviewOption}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('ShowPosters')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="showPosters"
              value={showPosters}
              helpText={translate('CollectionShowPostersHelpText')}
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

export default CollectionOverviewOptionsModalContent;
