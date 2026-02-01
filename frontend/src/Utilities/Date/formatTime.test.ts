import formatTime from './formatTime';

describe('formatTime', () => {
  it('should return empty string for null', () => {
    expect(formatTime(null, 'h(:mm)A')).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(formatTime(undefined, 'h(:mm)A')).toBe('');
  });

  it('should return empty string for empty string', () => {
    expect(formatTime('', 'h(:mm)A')).toBe('');
  });

  it('should format time without minutes when minute is zero', () => {
    const result = formatTime('2024-01-15T14:00:00Z', 'h(:mm)A');
    expect(result).not.toContain(':00');
  });

  it('should format time with minutes when minute is not zero', () => {
    const result = formatTime('2024-01-15T14:30:00Z', 'h(:mm)A');
    expect(result).toContain(':30');
  });

  it('should include minutes when includeMinuteZero is true', () => {
    const result = formatTime('2024-01-15T14:00:00Z', 'h(:mm)A', {
      includeMinuteZero: true,
    });
    expect(result).toContain(':00');
  });

  it('should include seconds when includeSeconds is true', () => {
    const result = formatTime('2024-01-15T14:30:45Z', 'h(:mm)A', {
      includeSeconds: true,
    });
    expect(result).toContain(':45');
  });

  it('should format 24-hour time correctly', () => {
    const result = formatTime('2024-01-15T14:30:00Z', 'HH:mm');
    expect(result).toMatch(/\d{2}:\d{2}/);
  });
});
