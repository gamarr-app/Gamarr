import React from 'react';
import translate from 'Utilities/String/translate';
import EnhancedSelectInput, {
  EnhancedSelectInputProps,
  EnhancedSelectInputValue,
} from './EnhancedSelectInput';

export interface AvailabilitySelectInputProps
  extends Omit<
    EnhancedSelectInputProps<EnhancedSelectInputValue<string>, string>,
    'values'
  > {
  includeNoChange?: boolean;
  includeNoChangeDisabled?: boolean;
  includeMixed?: boolean;
}

interface IGameAvailabilityOption {
  key: string;
  value: string;
  isDisabled?: boolean;
}

const gameAvailabilityOptions: IGameAvailabilityOption[] = [
  {
    key: 'announced',
    get value() {
      return translate('Announced');
    },
  },
  {
    key: 'inCinemas',
    get value() {
      return translate('InDevelopment');
    },
  },
  {
    key: 'released',
    get value() {
      return translate('Released');
    },
  },
];

function AvailabilitySelectInput(props: AvailabilitySelectInputProps) {
  const {
    includeNoChange = false,
    includeNoChangeDisabled = true,
    includeMixed = false,
  } = props;

  const values: EnhancedSelectInputValue<string>[] = [
    ...gameAvailabilityOptions,
  ];

  if (includeNoChange) {
    values.unshift({
      key: 'noChange',
      value: translate('NoChange'),
      isDisabled: includeNoChangeDisabled,
    });
  }

  if (includeMixed) {
    values.unshift({
      key: 'mixed',
      value: `(${translate('Mixed')})`,
      isDisabled: true,
    });
  }

  return <EnhancedSelectInput {...props} values={values} />;
}

export default AvailabilitySelectInput;
