/* eslint-disable filenames/match-exported */
import dark from './dark';
import light from './light';
import Theme from './Theme';

const defaultDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
const auto = defaultDark ? dark : light;

interface Themes {
  [key: string]: Theme;
  auto: Theme;
  light: Theme;
  dark: Theme;
}

const themes: Themes = {
  auto,
  light,
  dark,
};

export default themes;
