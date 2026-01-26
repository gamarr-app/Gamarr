interface CaptchaAppState {
  refreshing: boolean;
  token: string | null;
  siteKey: string | null;
  secretToken: string | null;
  ray: string | null;
  stoken: string | null;
  responseUrl: string | null;
}

export default CaptchaAppState;
