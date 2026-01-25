import MobileDetect from 'mobile-detect';

const mobileDetect = new MobileDetect(window.navigator.userAgent);

export function isMobile(): boolean {
  return mobileDetect.mobile() != null;
}

export function isIOS(): boolean {
  return mobileDetect.is('iOS');
}

export function isFirefox(): boolean {
  return window.navigator.userAgent.toLowerCase().indexOf('firefox/') >= 0;
}
