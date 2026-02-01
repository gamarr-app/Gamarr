import formatBytes from './formatBytes';

describe('formatBytes', () => {
  it('should return empty string for NaN input', () => {
    expect(formatBytes('not a number')).toBe('');
  });

  it('should format zero bytes', () => {
    expect(formatBytes(0)).toBe('0 B');
  });

  it('should format bytes', () => {
    expect(formatBytes(500)).toBe('500 B');
  });

  it('should format kilobytes', () => {
    const result = formatBytes(1024);
    expect(result).toBe('1 KiB');
  });

  it('should format megabytes', () => {
    const result = formatBytes(1048576);
    expect(result).toBe('1 MiB');
  });

  it('should format gigabytes', () => {
    const result = formatBytes(1073741824);
    expect(result).toBe('1 GiB');
  });

  it('should handle string number input', () => {
    const result = formatBytes('1048576');
    expect(result).toBe('1 MiB');
  });

  it('should round to one decimal place', () => {
    const result = formatBytes(1572864); // 1.5 MiB
    expect(result).toBe('1.5 MiB');
  });
});
