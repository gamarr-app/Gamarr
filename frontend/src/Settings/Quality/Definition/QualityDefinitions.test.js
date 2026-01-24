import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import QualityDefinitions from './QualityDefinitions';

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key) => key
}));

jest.mock('Components/FieldSet', () => {
  return function MockFieldSet({ legend, children }) {
    return (
      <fieldset>
        <legend>{legend}</legend>
        {children}
      </fieldset>
    );
  };
});

jest.mock('Components/Page/PageSectionContent', () => {
  return function MockPageSectionContent({ children }) {
    return <div data-testid="page-section-content">{children}</div>;
  };
});

jest.mock('./QualityDefinitionConnector', () => {
  return function MockQualityDefinitionConnector({ id, title, quality }) {
    return (
      <div data-testid={`quality-def-${id}`}>
        {quality.name} - {title}
      </div>
    );
  };
});

describe('QualityDefinitions', () => {
  const defaultProps = {
    isFetching: false,
    error: null,
    items: [
      { id: 1, quality: { name: 'Repack' }, title: 'Repack' },
      { id: 2, quality: { name: 'GOG' }, title: 'GOG' },
      { id: 3, quality: { name: 'Scene' }, title: 'Scene' }
    ],
    advancedSettings: false
  };

  it('should render header with Quality and Title columns only', () => {
    render(<QualityDefinitions {...defaultProps} />);

    expect(screen.getByText('Quality')).toBeInTheDocument();
    expect(screen.getByText('Title')).toBeInTheDocument();
  });

  it('should not render Size Limit or Megabytes Per Minute headers', () => {
    render(<QualityDefinitions {...defaultProps} />);

    expect(screen.queryByText('SizeLimit')).not.toBeInTheDocument();
    expect(screen.queryByText('MegabytesPerMinute')).not.toBeInTheDocument();
  });

  it('should render a QualityDefinitionConnector for each item', () => {
    render(<QualityDefinitions {...defaultProps} />);

    expect(screen.getByTestId('quality-def-1')).toBeInTheDocument();
    expect(screen.getByTestId('quality-def-2')).toBeInTheDocument();
    expect(screen.getByTestId('quality-def-3')).toBeInTheDocument();
  });

  it('should render the field set legend', () => {
    render(<QualityDefinitions {...defaultProps} />);

    expect(screen.getByText('QualityDefinitions')).toBeInTheDocument();
  });

  it('should not render size limit help text', () => {
    const { container } = render(<QualityDefinitions {...defaultProps} />);

    expect(container.querySelector('.sizeLimitHelpTextContainer')).toBeNull();
    expect(container.querySelector('.sizeLimitHelpText')).toBeNull();
  });
});
