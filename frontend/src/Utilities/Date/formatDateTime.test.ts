import formatDate from './formatDate';
import formatDateTime from './formatDateTime';

describe('formatDateTime', () => {
  it('should return empty string for null', () => {
    expect(formatDateTime(null, 'MMM D YYYY', 'h(:mm)a')).toBe('');
  });

  it('should not throw when both formats are undefined', () => {
    expect(() =>
      formatDateTime('2024-01-15T14:30:00Z', undefined, undefined)
    ).not.toThrow();
  });

  it('should not throw when timeFormat is undefined', () => {
    expect(() =>
      formatDateTime('2024-01-15T14:30:00Z', 'MMM D YYYY', undefined)
    ).not.toThrow();
  });
});

describe('formatDate', () => {
  it('should not throw when dateFormat is undefined', () => {
    expect(formatDate('2024-01-15T14:30:00Z', undefined)).toContain('2024');
  });
});
