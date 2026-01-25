import $ from 'jquery';

interface Resolutions {
  desktopLarge: number;
  desktop: number;
  tablet: number;
  mobile: number;
}

interface ResolutionUtility {
  resolutions: Resolutions;
  isDesktopLarge(): boolean;
  isDesktop(): boolean;
  isTablet(): boolean;
  isMobile(): boolean;
}

const resolutionUtility: ResolutionUtility = {
  resolutions: {
    desktopLarge: 1200,
    desktop: 992,
    tablet: 768,
    mobile: 480,
  },

  isDesktopLarge() {
    return ($(window).width() ?? 0) < this.resolutions.desktopLarge;
  },

  isDesktop() {
    return ($(window).width() ?? 0) < this.resolutions.desktop;
  },

  isTablet() {
    return ($(window).width() ?? 0) < this.resolutions.tablet;
  },

  isMobile() {
    return ($(window).width() ?? 0) < this.resolutions.mobile;
  },
};

export = resolutionUtility;
