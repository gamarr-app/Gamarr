import padNumber from './padNumber';

describe('padNumber', () => {
  it('should return empty string for null', () => {
    expect(padNumber(null, 2)).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(padNumber(undefined, 2)).toBe('');
  });

  it('should pad single digit number to width 2', () => {
    expect(padNumber(5, 2)).toBe('05');
  });

  it('should not pad number already at width', () => {
    expect(padNumber(12, 2)).toBe('12');
  });

  it('should not pad number longer than width', () => {
    expect(padNumber(123, 2)).toBe('123');
  });

  it('should pad with custom character', () => {
    expect(padNumber(5, 3, ' ')).toBe('  5');
  });

  it('should handle string input', () => {
    expect(padNumber('7', 2)).toBe('07');
  });

  it('should handle zero', () => {
    expect(padNumber(0, 2)).toBe('00');
  });
});
