import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';
import FileEditModalContent from './FileEditModalContent';

import '@testing-library/jest-dom';

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

jest.mock('Components/Modal/ModalContent', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="modal-content">{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalHeader', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="modal-header">{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalBody', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="modal-body">{children}</div>
  ),
}));

jest.mock('Components/Modal/ModalFooter', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="modal-footer">{children}</div>
  ),
}));

jest.mock('Components/Form/Form', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <form data-testid="form">{children}</form>
  ),
}));

jest.mock('Components/Form/FormGroup', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div>{children}</div>
  ),
}));

jest.mock('Components/Form/FormLabel', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <label>{children}</label>
  ),
}));

// Mirrors the real inputs: the language and quality selects are single-select,
// so they report a bare number, never an array.
jest.mock('Components/Form/FormInputGroup', () => ({
  __esModule: true,
  default: ({
    name,
    type,
    onChange,
  }: {
    name: string;
    type: string;
    onChange: (change: { name: string; value: unknown }) => void;
  }) => (
    <input
      data-testid={`form-input-${name}`}
      data-type={type}
      onChange={(e) =>
        onChange({
          name,
          value: type === 'text' ? e.target.value : Number(e.target.value),
        })
      }
    />
  ),
}));

jest.mock('Components/Link/Button', () => ({
  __esModule: true,
  default: ({
    children,
    onPress,
  }: {
    children: React.ReactNode;
    onPress: () => void;
  }) => (
    <button data-testid="button" onClick={onPress}>
      {children}
    </button>
  ),
}));

jest.mock('Components/Loading/LoadingIndicator', () => ({
  __esModule: true,
  default: () => <div data-testid="loading-indicator" />,
}));

jest.mock('Components/Alert', () => ({
  __esModule: true,
  default: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="alert">{children}</div>
  ),
}));

const languages = [
  { id: 1, name: 'English' },
  { id: 5, name: 'German' },
];

const qualities = [
  { id: 10, name: 'Repack' },
  { id: 20, name: 'Scene' },
];

function renderContent(
  props: Partial<React.ComponentProps<typeof FileEditModalContent>> = {}
) {
  const onSaveInputs = jest.fn();

  render(
    <FileEditModalContent
      isFetching={false}
      isPopulated={true}
      error={null}
      qualityId={10}
      qualities={qualities}
      languageIds={[1]}
      languages={languages}
      indexerFlags={0}
      edition=""
      releaseGroup=""
      version=""
      relativePath="Some.Game.iso"
      onSaveInputs={onSaveInputs}
      onModalClose={jest.fn()}
      {...props}
    />
  );

  return { onSaveInputs };
}

function save() {
  fireEvent.click(screen.getByText('Save'));
}

describe('FileEditModalContent', () => {
  it('saves the initial language ids unchanged', () => {
    const { onSaveInputs } = renderContent();

    save();

    expect(onSaveInputs).toHaveBeenCalledWith(
      expect.objectContaining({ languageIds: [1] })
    );
  });

  it('wraps the single-select language value in an array', () => {
    const { onSaveInputs } = renderContent();

    fireEvent.change(screen.getByTestId('form-input-languageIds'), {
      target: { value: '5' },
    });

    save();

    expect(onSaveInputs).toHaveBeenCalledWith(
      expect.objectContaining({ languageIds: [5] })
    );
  });

  it('saves a language the consumer can iterate over', () => {
    const { onSaveInputs } = renderContent();

    fireEvent.change(screen.getByTestId('form-input-languageIds'), {
      target: { value: '5' },
    });

    save();

    // The connector calls forEach on this, so a bare number throws a
    // TypeError and the save is silently lost.
    const { languageIds } = onSaveInputs.mock.calls[0][0];

    expect(() => languageIds.forEach(() => {})).not.toThrow();
  });

  it('saves the edited text fields', () => {
    const { onSaveInputs } = renderContent();

    fireEvent.change(screen.getByTestId('form-input-edition'), {
      target: { value: 'Deluxe' },
    });
    fireEvent.change(screen.getByTestId('form-input-releaseGroup'), {
      target: { value: 'RZR' },
    });
    fireEvent.change(screen.getByTestId('form-input-version'), {
      target: { value: '1.2.3' },
    });

    save();

    expect(onSaveInputs).toHaveBeenCalledWith(
      expect.objectContaining({
        edition: 'Deluxe',
        releaseGroup: 'RZR',
        version: '1.2.3',
      })
    );
  });

  it('shows a loading indicator while fetching', () => {
    renderContent({ isFetching: true, isPopulated: false });

    expect(screen.getByTestId('loading-indicator')).toBeInTheDocument();
    expect(screen.queryByTestId('form')).not.toBeInTheDocument();
  });
});
