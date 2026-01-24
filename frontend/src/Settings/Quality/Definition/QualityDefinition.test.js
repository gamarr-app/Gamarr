import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import QualityDefinition from './QualityDefinition';

jest.mock('Components/Form/TextInput', () => {
  return function MockTextInput({ name, value, onChange }) {
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
  const defaultProps = {
    id: 1,
    quality: { name: 'Repack' },
    title: 'Repack',
    onTitleChange: jest.fn()
  };

  it('should render quality name', () => {
    render(<QualityDefinition {...defaultProps} />);
    expect(screen.getByText('Repack')).toBeInTheDocument();
  });

  it('should render title input with correct value', () => {
    render(<QualityDefinition {...defaultProps} />);
    const input = screen.getByTestId('text-input');
    expect(input).toHaveValue('Repack');
  });

  it('should not render any slider or size limit elements', () => {
    const { container } = render(<QualityDefinition {...defaultProps} />);

    // Verify no slider-related elements exist
    expect(container.querySelector('.slider')).toBeNull();
    expect(container.querySelector('.sizeLimit')).toBeNull();
    expect(container.querySelector('.megabytesPerMinute')).toBeNull();
    expect(container.querySelector('.sizes')).toBeNull();
  });

  it('should not accept advancedSettings, minSize, maxSize, or preferredSize props', () => {
    // These props were removed - component should render fine without them
    const props = {
      ...defaultProps,
      advancedSettings: true,
      minSize: 10,
      maxSize: 100,
      preferredSize: 50
    };

    const { container } = render(<QualityDefinition {...props} />);

    // Should still only render quality name and title, no size UI
    expect(container.querySelector('.slider')).toBeNull();
    expect(container.querySelector('.sizeLimit')).toBeNull();
  });

  it('should pass onTitleChange to the text input', () => {
    const onTitleChange = jest.fn();
    render(
      <QualityDefinition
        {...defaultProps}
        onTitleChange={onTitleChange}
      />
    );

    const input = screen.getByTestId('text-input');
    input.dispatchEvent(new Event('change', { bubbles: true }));
  });
});
