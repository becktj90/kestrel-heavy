// On-disk key rules from docs/SAVE_BRIDGE.md. The WKWebView localStorage store does not
// enforce these; the native file store does.
export const KEY_RE = /^[A-Za-z0-9._-]+$/;
export const PREFIX = 'kh.save.';
export const MAX_KEY_LENGTH = 200;
export const MAX_VALUE_BYTES = 1000000;

export function checkKey(key) {
  return typeof key === 'string' && key.startsWith(PREFIX) && key.length <= MAX_KEY_LENGTH && KEY_RE.test(key);
}
