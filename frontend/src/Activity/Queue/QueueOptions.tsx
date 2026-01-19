import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import { gotoQueuePage, setQueueOption } from 'Store/Actions/queueActions';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';

function QueueOptions() {
  const dispatch = useDispatch();
  const { includeUnknownGameItems } = useSelector(
    (state: AppState) => state.queue.options
  );

  const handleOptionChange = useCallback(
    ({ name, value }: InputChanged<boolean>) => {
      dispatch(
        setQueueOption({
          [name]: value,
        })
      );

      if (name === 'includeUnknownGameItems') {
        dispatch(gotoQueuePage({ page: 1 }));
      }
    },
    [dispatch]
  );

  return (
    <FormGroup>
      <FormLabel>{translate('ShowUnknownGameItems')}</FormLabel>

      <FormInputGroup
        type={inputTypes.CHECK}
        name="includeUnknownGameItems"
        value={includeUnknownGameItems}
        helpText={translate('ShowUnknownGameItemsHelpText')}
        onChange={handleOptionChange}
      />
    </FormGroup>
  );
}

export default QueueOptions;
