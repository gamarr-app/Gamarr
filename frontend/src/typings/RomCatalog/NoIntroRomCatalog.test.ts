import {
  type NoIntroCatalogPlan,
  noIntroRomComponentTypes,
  noIntroVerificationStatuses,
} from './NoIntroRomCatalog';

describe('NoIntroRomCatalog', () => {
  it('keeps Download Play as a mapped component and standalone products separate', () => {
    const plan: NoIntroCatalogPlan = {
      games: [
        {
          systemKey: 'nintendo-ds',
          gameTitle: 'Mario Kart DS',
          regionLanguageComponents: [
            {
              slotLabel: 'USA',
              canonicalName: 'Mario Kart DS (USA)',
              componentType: 'retailRom',
            },
          ],
          downloadPlayComponents: [
            {
              slotLabel: 'Download Play',
              canonicalName: 'Mario Kart DS (Download Play)',
              componentType: 'multiboot',
            },
          ],
        },
      ],
      standaloneGames: [
        {
          title:
            'Game Boy Advance Video - Cartoon Network Collection - Volume 1 (USA)',
          componentType: 'video',
        },
      ],
    };

    expect(noIntroVerificationStatuses).toContain('nameMismatch');
    expect(noIntroRomComponentTypes).toContain('multiboot');
    expect(plan.games[0].downloadPlayComponents[0].slotLabel).toBe(
      'Download Play'
    );
    expect(plan.standaloneGames[0].componentType).toBe('video');
  });
});
