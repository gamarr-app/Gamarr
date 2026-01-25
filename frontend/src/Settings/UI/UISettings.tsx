import React, { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes, kinds } from 'Helpers/Props';
import SettingsToolbar from 'Settings/SettingsToolbar';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import {
  fetchUISettings,
  saveUISettings,
  setUISettingsValue,
} from 'Store/Actions/settingsActions';
import createLanguagesSelector from 'Store/Selectors/createLanguagesSelector';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import themes from 'Styles/Themes';
import { InputChanged } from 'typings/inputs';
import titleCase from 'Utilities/String/titleCase';
import translate from 'Utilities/String/translate';
import styles from './UISettings.css';

const SECTION = 'ui';
const FILTER_LANGUAGES = ['Any', 'Unknown', 'Original'];

export const firstDayOfWeekOptions = [
  {
    key: 0,
    get value() {
      return translate('Sunday');
    },
  },
  {
    key: 1,
    get value() {
      return translate('Monday');
    },
  },
];

export const weekColumnOptions = [
  { key: 'ddd M/D', value: 'Tue 3/25', hint: 'ddd M/D' },
  { key: 'ddd MM/DD', value: 'Tue 03/25', hint: 'ddd MM/DD' },
  { key: 'ddd D/M', value: 'Tue 25/3', hint: 'ddd D/M' },
  { key: 'ddd DD/MM', value: 'Tue 25/03', hint: 'ddd DD/MM' },
];

const shortDateFormatOptions = [
  { key: 'MMM D YYYY', value: 'Mar 25 2014', hint: 'MMM D YYYY' },
  { key: 'DD MMM YYYY', value: '25 Mar 2014', hint: 'DD MMM YYYY' },
  { key: 'MM/D/YYYY', value: '03/25/2014', hint: 'MM/D/YYYY' },
  { key: 'MM/DD/YYYY', value: '03/25/2014', hint: 'MM/DD/YYYY' },
  { key: 'DD/MM/YYYY', value: '25/03/2014', hint: 'DD/MM/YYYY' },
  { key: 'YYYY-MM-DD', value: '2014-03-25', hint: 'YYYY-MM-DD' },
];

const longDateFormatOptions = [
  { key: 'dddd, MMMM D YYYY', value: 'Tuesday, March 25, 2014' },
  { key: 'dddd, D MMMM YYYY', value: 'Tuesday, 25 March, 2014' },
];

export const timeFormatOptions = [
  { key: 'h(:mm)a', value: '5pm/5:30pm' },
  { key: 'HH:mm', value: '17:00/17:30' },
];

export const gameRuntimeFormatOptions = [
  { key: 'hoursMinutes', value: '1h 15m' },
  { key: 'minutes', value: '75 mins' },
];

function createFilteredLanguagesSelector() {
  return createSelector(createLanguagesSelector(), (languages) => {
    if (!languages || !languages.items) {
      return {
        isFetching: false,
        isPopulated: false,
        items: [],
        error: undefined as Error | undefined,
      };
    }

    const newItems = languages.items
      .filter((lang: { name: string }) => !FILTER_LANGUAGES.includes(lang.name))
      .map((item: { id: number; name: string }) => ({
        key: item.id,
        value: item.name,
      }));

    return {
      ...languages,
      items: newItems,
    };
  });
}

function UISettings() {
  const dispatch = useDispatch();

  const filteredLanguagesSelector = useMemo(
    createFilteredLanguagesSelector,
    []
  );

  const {
    isFetching: isFetchingSettings,
    isSaving,
    error: settingsError,
    settings,
    hasSettings,
    hasPendingChanges,
  } = useSelector(createSettingsSectionSelector(SECTION));

  const languagesData = useSelector(filteredLanguagesSelector);
  const languages = languagesData.items || [];
  const isFetching = isFetchingSettings || languagesData.isFetching;
  const error = settingsError || languagesData.error;

  const handleInputChange = useCallback(
    (change: InputChanged) => {
      // @ts-expect-error - actions aren't typed
      dispatch(setUISettingsValue(change));
    },
    [dispatch]
  );

  const handleSavePress = useCallback(() => {
    dispatch(saveUISettings());
  }, [dispatch]);

  useEffect(() => {
    dispatch(fetchUISettings());

    return () => {
      dispatch(clearPendingChanges({ section: `settings.${SECTION}` }));
    };
  }, [dispatch]);

  const themeOptions = Object.keys(themes).map((theme) => ({
    key: theme,
    value: titleCase(theme),
  }));

  const uiLanguages = languages.filter(
    (item: { value: string }) => item.value !== 'Original'
  );

  return (
    <PageContent className={styles.uiSettings} title={translate('UiSettings')}>
      <SettingsToolbar
        isSaving={isSaving}
        hasPendingChanges={hasPendingChanges}
        onSavePress={handleSavePress}
      />

      <PageContentBody>
        {isFetching ? <LoadingIndicator /> : null}

        {!isFetching && error ? (
          <Alert kind={kinds.DANGER}>{translate('UiSettingsLoadError')}</Alert>
        ) : null}

        {hasSettings && !isFetching && !error ? (
          <Form id="uiSettings">
            <FieldSet legend={translate('Calendar')}>
              <FormGroup>
                <FormLabel>{translate('FirstDayOfWeek')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="firstDayOfWeek"
                  values={firstDayOfWeekOptions}
                  onChange={handleInputChange}
                  {...settings.firstDayOfWeek}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('WeekColumnHeader')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="calendarWeekColumnHeader"
                  values={weekColumnOptions}
                  helpText={translate('WeekColumnHeaderHelpText')}
                  onChange={handleInputChange}
                  {...settings.calendarWeekColumnHeader}
                />
              </FormGroup>
            </FieldSet>

            <FieldSet legend={translate('Games')}>
              <FormGroup>
                <FormLabel>{translate('RuntimeFormat')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="gameRuntimeFormat"
                  values={gameRuntimeFormatOptions}
                  onChange={handleInputChange}
                  {...settings.gameRuntimeFormat}
                />
              </FormGroup>
            </FieldSet>

            <FieldSet legend={translate('Dates')}>
              <FormGroup>
                <FormLabel>{translate('ShortDateFormat')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="shortDateFormat"
                  values={shortDateFormatOptions}
                  onChange={handleInputChange}
                  {...settings.shortDateFormat}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('LongDateFormat')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="longDateFormat"
                  values={longDateFormatOptions}
                  onChange={handleInputChange}
                  {...settings.longDateFormat}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('TimeFormat')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="timeFormat"
                  values={timeFormatOptions}
                  onChange={handleInputChange}
                  {...settings.timeFormat}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('ShowRelativeDates')}</FormLabel>
                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="showRelativeDates"
                  helpText={translate('ShowRelativeDatesHelpText')}
                  onChange={handleInputChange}
                  {...settings.showRelativeDates}
                />
              </FormGroup>
            </FieldSet>

            <FieldSet legend={translate('Style')}>
              <FormGroup>
                <FormLabel>{translate('Theme')}</FormLabel>
                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="theme"
                  helpText={translate('ThemeHelpText')}
                  values={themeOptions}
                  onChange={handleInputChange}
                  {...settings.theme}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('EnableColorImpairedMode')}</FormLabel>
                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="enableColorImpairedMode"
                  helpText={translate('EnableColorImpairedModeHelpText')}
                  onChange={handleInputChange}
                  {...settings.enableColorImpairedMode}
                />
              </FormGroup>
            </FieldSet>

            <FieldSet legend={translate('Language')}>
              <FormGroup>
                <FormLabel>{translate('GameInfoLanguage')}</FormLabel>
                <FormInputGroup
                  type={inputTypes.LANGUAGE_SELECT}
                  name="gameInfoLanguage"
                  values={languages}
                  helpText={translate('GameInfoLanguageHelpText')}
                  helpTextWarning={translate('GameInfoLanguageHelpTextWarning')}
                  onChange={handleInputChange}
                  {...settings.gameInfoLanguage}
                  errors={
                    languages.some(
                      (language: { key: number }) =>
                        language.key === settings.gameInfoLanguage.value
                    )
                      ? settings.gameInfoLanguage.errors
                      : [
                          ...settings.gameInfoLanguage.errors,
                          {
                            message: translate(
                              'InvalidGameInfoLanguageLanguage'
                            ),
                          },
                        ]
                  }
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('UiLanguage')}</FormLabel>
                <FormInputGroup
                  type={inputTypes.LANGUAGE_SELECT}
                  name="uiLanguage"
                  values={uiLanguages}
                  helpText={translate('UiLanguageHelpText')}
                  helpTextWarning={translate('BrowserReloadRequired')}
                  onChange={handleInputChange}
                  {...settings.uiLanguage}
                  errors={
                    languages.some(
                      (language: { key: number }) =>
                        language.key === settings.uiLanguage.value
                    )
                      ? settings.uiLanguage.errors
                      : [
                          ...settings.uiLanguage.errors,
                          { message: translate('InvalidUILanguage') },
                        ]
                  }
                />
              </FormGroup>
            </FieldSet>
          </Form>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default UISettings;
