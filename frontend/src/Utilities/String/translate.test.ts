jest.mock('Utilities/createAjaxRequest', () => ({
  __esModule: true,
  default: () => ({ request: Promise.resolve({ Strings: {} }) }),
}));

import translate from './translate';

describe('translate', () => {
  beforeEach(() => {
    (window as any).Gamarr = { isProduction: true };
  });

  it('should return the key when no translation exists', () => {
    expect(translate('UnknownKey')).toBe('UnknownKey');
  });

  it('should replace named tokens', () => {
    expect(translate('{name} is great', { name: 'Gamarr' })).toBe(
      'Gamarr is great'
    );
  });

  it('should replace multiple tokens', () => {
    expect(translate('{count} of {total}', { count: 5, total: 10 })).toBe(
      '5 of 10'
    );
  });

  it('should inject appName token automatically', () => {
    expect(translate('{appName}')).toBe('Gamarr');
  });

  it('should leave unmatched tokens in place', () => {
    expect(translate('{unknown}')).toBe('{unknown}');
  });
});
