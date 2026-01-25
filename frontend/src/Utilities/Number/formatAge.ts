import translate from 'Utilities/String/translate';

function formatAge(
  age: number,
  ageHours: number | string,
  ageMinutes?: number | string
): string {
  age = Math.round(age);
  const parsedAgeHours = parseFloat(String(ageHours));
  const parsedAgeMinutes = ageMinutes ? parseFloat(String(ageMinutes)) : 0;

  if (age < 2 && parsedAgeHours) {
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

  return `${age} ${
    age === 1 ? translate('FormatAgeDay') : translate('FormatAgeDays')
  }`;
}

export default formatAge;
