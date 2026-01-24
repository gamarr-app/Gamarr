import { fireEvent,render, screen } from '@testing-library/react';
import React from 'react';
import QualityDefinition from './QualityDefinition';

import '@testing-library/jest-dom';

const mockDispatch = jest.fn();

jest.mock('react-redux', () => ({
  useDispatch: () => mockDispatch
}));

jest.mock('Store/Actions/baseActions', () => ({
  clearPendingChanges: jest.fn((payload) => ({ type: 'CLEAR', payload }))
}));

jest.mock('Store/Actions/settingsActions', () => ({
  setQualityDefinitionValue: jest.fn((payload) => ({ type: 'SET_VALUE', payload }))
}));

jest.mock('Components/Form/TextInput', () => {
  return function MockTextInput({ name, value, onChange }: any) {
    return (
      <input
        data-testid="text-input"
        name={name}
        value={value}
        onChange={(e) => onChange({ name, value: e.target.value })}
      />
    );
  };
});

describe('QualityDefinition', () => {
  beforeEach(() => {
    mockDispatch.mockClear();
  });

  it('should render quality name', () => {
    render(
      <QualityDefinition id={1} quality={{ name: 'Repack' }} title="Repack" />
    );
    expect(screen.getByText('Repack')).toBeInTheDocument();
  });

  it('should render title input with correct value', () => {
    render(
      <QualityDefinition id={1} quality={{ name: 'Repack' }} title="Repack" />
    );
    const input = screen.getByTestId('text-input');
    expect(input).toHaveValue('Repack');
  });

  it('should not render any slider or size limit elements', () => {
    const { container } = render(
      <QualityDefinition id={1} quality={{ name: 'Repack' }} title="Repack" />
    );
    expect(container.querySelector('.slider')).toBeNull();
    expect(container.querySelector('.sizeLimit')).toBeNull();
    expect(container.querySelector('.megabytesPerMinute')).toBeNull();
  });

  it('should dispatch setQualityDefinitionValue on title change', () => {
    const { setQualityDefinitionValue } = require('Store/Actions/settingsActions');

    render(
      <QualityDefinition id={5} quality={{ name: 'GOG' }} title="GOG" />
    );

    const input = screen.getByTestId('text-input');
    fireEvent.change(input, { target: { value: 'GOG Rip' } });

    expect(setQualityDefinitionValue).toHaveBeenCalledWith({
      id: 5,
      name: 'title',
      value: 'GOG Rip',
    });
    expect(mockDispatch).toHaveBeenCalled();
  });

  it('should dispatch clearPendingChanges on unmount', () => {
    const { clearPendingChanges } = require('Store/Actions/baseActions');

    const { unmount } = render(
      <QualityDefinition id={1} quality={{ name: 'Repack' }} title="Repack" />
    );

    unmount();

    expect(clearPendingChanges).toHaveBeenCalledWith({
      section: 'settings.qualityDefinitions',
    });
    expect(mockDispatch).toHaveBeenCalled();
  });
});
