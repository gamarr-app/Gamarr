import { render, screen } from '@testing-library/react';
import QualityDefinitions from './QualityDefinitions';

import '@testing-library/jest-dom';

const mockDispatch = jest.fn();
const mockSelector = jest.fn();

jest.mock('react-redux', () => ({
  useDispatch: () => mockDispatch,
  useSelector: (selector: any) => mockSelector(selector),
}));

jest.mock('reselect', () => ({
  createSelector: (...args: any[]) => {
    const resultFunc = args[args.length - 1];
    return (state: any) => resultFunc(state);
  },
}));

jest.mock('Store/Actions/settingsActions', () => ({
  fetchQualityDefinitions: jest.fn(() => ({ type: 'FETCH' })),
  saveQualityDefinitions: jest.fn(() => ({ type: 'SAVE' })),
}));

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

jest.mock('Components/FieldSet', () => {
  return function MockFieldSet({ legend, children }: any) {
    return (
      <fieldset>
        <legend>{legend}</legend>
        {children}
      </fieldset>
    );
  };
});

jest.mock('Components/Page/PageSectionContent', () => {
  return function MockPageSectionContent({ children }: any) {
    return <div data-testid="page-section-content">{children}</div>;
  };
});

jest.mock('./QualityDefinition', () => {
  return function MockQualityDefinition({ id, title, quality }: any) {
    return (
      <div data-testid={`quality-def-${id}`}>
        {quality.name} - {title}
      </div>
    );
  };
});

describe('QualityDefinitions', () => {
  const mockSetChildSave = jest.fn();
  const mockOnChildStateChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    mockSelector.mockReturnValue({
      isFetching: false,
      isPopulated: true,
      error: null,
      isSaving: false,
      hasPendingChanges: false,
      items: [
        { id: 1, quality: { name: 'Repack' }, title: 'Repack' },
        { id: 2, quality: { name: 'GOG' }, title: 'GOG' },
        { id: 3, quality: { name: 'Scene' }, title: 'Scene' },
      ],
    });
  });

  it('should render header with Quality and Title columns', () => {
    render(
      <QualityDefinitions
        setChildSave={mockSetChildSave}
        onChildStateChange={mockOnChildStateChange}
      />
    );

    expect(screen.getByText('Quality')).toBeInTheDocument();
    expect(screen.getByText('Title')).toBeInTheDocument();
  });

  it('should not render Size Limit or Megabytes Per Minute headers', () => {
    render(
      <QualityDefinitions
        setChildSave={mockSetChildSave}
        onChildStateChange={mockOnChildStateChange}
      />
    );

    expect(screen.queryByText('SizeLimit')).not.toBeInTheDocument();
    expect(screen.queryByText('MegabytesPerMinute')).not.toBeInTheDocument();
  });

  it('should render a QualityDefinition for each item', () => {
    render(
      <QualityDefinitions
        setChildSave={mockSetChildSave}
        onChildStateChange={mockOnChildStateChange}
      />
    );

    expect(screen.getByTestId('quality-def-1')).toBeInTheDocument();
    expect(screen.getByTestId('quality-def-2')).toBeInTheDocument();
    expect(screen.getByTestId('quality-def-3')).toBeInTheDocument();
  });

  it('should render the field set legend', () => {
    render(
      <QualityDefinitions
        setChildSave={mockSetChildSave}
        onChildStateChange={mockOnChildStateChange}
      />
    );

    expect(screen.getByText('QualityDefinitions')).toBeInTheDocument();
  });

  it('should dispatch fetchQualityDefinitions on mount', () => {
    render(
      <QualityDefinitions
        setChildSave={mockSetChildSave}
        onChildStateChange={mockOnChildStateChange}
      />
    );

    expect(mockDispatch).toHaveBeenCalledWith({ type: 'FETCH' });
  });

  it('should call setChildSave on mount', () => {
    render(
      <QualityDefinitions
        setChildSave={mockSetChildSave}
        onChildStateChange={mockOnChildStateChange}
      />
    );

    expect(mockSetChildSave).toHaveBeenCalled();
  });

  it('should call onChildStateChange with save state', () => {
    render(
      <QualityDefinitions
        setChildSave={mockSetChildSave}
        onChildStateChange={mockOnChildStateChange}
      />
    );

    expect(mockOnChildStateChange).toHaveBeenCalledWith({
      isSaving: false,
      hasPendingChanges: false,
    });
  });
});
