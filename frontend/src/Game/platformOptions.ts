// The PlatformFamily enum (src/NzbDrone.Core/Games/GamePlatform.cs) as the API
// serializes it. Shared by the add dialog, the edit dialog and the per-platform
// root folder defaults so the three lists can't drift apart.
export interface PlatformOption {
  key: string;
  value: string;
}

const platformOptions: PlatformOption[] = [
  { key: 'unknown', value: 'Any' },
  { key: 'pc', value: 'PC (Windows)' },
  { key: 'linux', value: 'Linux' },
  { key: 'mac', value: 'macOS' },
  { key: 'playStation', value: 'PlayStation' },
  { key: 'sonyPS3', value: 'Sony PlayStation 3' },
  { key: 'sonyPSP', value: 'Sony PSP' },
  { key: 'sonyPSVita', value: 'Sony PlayStation Vita' },
  { key: 'xbox', value: 'Xbox' },
  { key: 'nintendo', value: 'Nintendo' },
  { key: 'nintendoSwitch', value: 'Nintendo Switch' },
  { key: 'nintendoWiiU', value: 'Nintendo Wii U' },
  { key: 'nintendoWii', value: 'Nintendo Wii' },
  { key: 'nintendo3DS', value: 'Nintendo 3DS' },
  { key: 'nintendoDSi', value: 'Nintendo DSi' },
  { key: 'nintendoDS', value: 'Nintendo DS' },
  { key: 'nintendoGBA', value: 'Nintendo Game Boy Advance' },
  { key: 'nintendoGBC', value: 'Nintendo Game Boy Color' },
  { key: 'nintendoGB', value: 'Nintendo Game Boy' },
  { key: 'nintendoNES', value: 'Nintendo Entertainment System' },
  { key: 'nintendoSNES', value: 'Super Nintendo Entertainment System' },
  { key: 'nintendoN64', value: 'Nintendo 64' },
  { key: 'nintendoFDS', value: 'Family Computer Disk System' },
  { key: 'nintendoVirtualBoy', value: 'Virtual Boy' },
  { key: 'nintendoPokemonMini', value: 'Pokemon Mini' },
];

export function getPlatformTitle(platform: string) {
  return platformOptions.find((p) => p.key === platform)?.value ?? platform;
}

/**
 * The single family a set of platforms unambiguously belongs to, or 'unknown'
 * when it spans more than one. Mirrors GamePlatform.UnambiguousFamily on the
 * backend, which is what an add without an explicit platform ends up with.
 */
export function getUnambiguousPlatform(
  platforms: { family?: string }[] | undefined
) {
  const [family, ...rest] = [
    ...new Set(
      (platforms ?? [])
        .map((p) => p.family)
        .filter((f): f is string => !!f && f !== 'unknown')
    ),
  ];

  return family && rest.length === 0 ? family : 'unknown';
}

export default platformOptions;
