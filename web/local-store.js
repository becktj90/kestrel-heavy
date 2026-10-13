/* Offline stub — the beckify site may replace this file with /toolbox/js/local-store.js.
   Kestrel Heavy already persists scores in localStorage via js/storage.js.
   recordGameScore is a no-op so a missing toolbox store never 404s or breaks a flight. */
window.recordGameScore = window.recordGameScore || function () { return Promise.resolve(null); };
