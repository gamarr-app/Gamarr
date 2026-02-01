import getProgressBarKind from './getProgressBarKind';

jest.mock('Helpers/Props', () => ({
  kinds: {
    DANGER: 'danger',
    DEFAULT: 'default',
    INVERSE: 'inverse',
    PRIMARY: 'primary',
    PURPLE: 'purple',
    SUCCESS: 'success',
    WARNING: 'warning',
  },
}));

describe('getProgressBarKind', () => {
  it('should return PURPLE when downloading', () => {
    expect(getProgressBarKind('released', true, false, true, true)).toBe(
      'purple'
    );
  });

  it('should return SUCCESS when has file and monitored', () => {
    expect(getProgressBarKind('released', true, true, true, false)).toBe(
      'success'
    );
  });

  it('should return DEFAULT when has file and not monitored', () => {
    expect(getProgressBarKind('released', false, true, true, false)).toBe(
      'default'
    );
  });

  it('should return INVERSE for deleted status', () => {
    expect(getProgressBarKind('deleted', true, false, false, false)).toBe(
      'inverse'
    );
  });

  it('should return DANGER when available and monitored but no file', () => {
    expect(getProgressBarKind('released', true, false, true, false)).toBe(
      'danger'
    );
  });

  it('should return WARNING when not monitored and no file', () => {
    expect(getProgressBarKind('released', false, false, false, false)).toBe(
      'warning'
    );
  });

  it('should return PRIMARY when monitored, not available, no file', () => {
    expect(getProgressBarKind('announced', true, false, false, false)).toBe(
      'primary'
    );
  });

  it('should prioritize downloading over has file', () => {
    expect(getProgressBarKind('released', true, true, true, true)).toBe(
      'purple'
    );
  });
});
