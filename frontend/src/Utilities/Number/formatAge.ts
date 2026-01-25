import translate from 'Utilities/String/translate';

function formatAge(
  age: number | string,
  ageHours: number | string,
  ageMinutes?: number | string
): string {
  const parsedAge = Math.round(parseFloat(String(age)));
  const parsedAgeHours = parseFloat(String(ageHours));
  const parsedAgeMinutes = ageMinutes ? parseFloat(String(ageMinutes)) : 0;

  if (parsedAge < 2 && parsedAgeHours) {
    if (parsedAgeHours < 2 && !!parsedAgeMinutes) {
      return `${parsedAgeMinutes.toFixed(0)} ${
        parsedAgeHours === 1
          ? translate('FormatAgeMinute')
          : translate('FormatAgeMinutes')
      }`;
    }

    return `${parsedAgeHours.toFixed(1)} ${
      parsedAgeHours === 1
        ? translate('FormatAgeHour')
        : translate('FormatAgeHours')
    }`;
  }

  return `${parsedAge} ${
    parsedAge === 1 ? translate('FormatAgeDay') : translate('FormatAgeDays')
  }`;
}

export default formatAge;
