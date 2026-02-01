import formatTimeSpan from './formatTimeSpan';

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string, tokens: Record<string, string | number> = {}) => {
    if (key === 'FormatTimeSpanDays') {
      return `${tokens.days}d ${tokens.time}`;
    }
    return key;
  },
}));

describe('formatTimeSpan', () => {
  it('should return empty string for null', () => {
    expect(formatTimeSpan(null)).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(formatTimeSpan(undefined)).toBe('');
  });

  it('should return empty string for empty string', () => {
    expect(formatTimeSpan('')).toBe('');
  });

  it('should format hours, minutes, and seconds', () => {
    // 2 hours, 30 minutes, 15 seconds in milliseconds
    const result = formatTimeSpan('02:30:15');
    expect(result).toBe('02:30:15');
  });

  it('should pad single digit values', () => {
    const result = formatTimeSpan('01:05:03');
    expect(result).toBe('01:05:03');
  });

  it('should format timespan with days', () => {
    // 1 day, 2 hours, 30 minutes
    const result = formatTimeSpan('1.02:30:00');
    expect(result).toBe('1d 02:30:00');
  });

  it('should handle zero timespan', () => {
    expect(formatTimeSpan(0)).toBe('');
  });
});
