import { reactive, watch } from "vue";
import { getPreferences, savePreferences } from "@/api/me";
import { useAuth } from "@/composables/useAuth";

const STORAGE_KEY = "novashelf:reader-preferences";

export const FONT_SIZES = [
  { value: "sm", label: "Smaller", proseClass: "prose-base" },
  { value: "md", label: "Default", proseClass: "prose-lg" },
  { value: "lg", label: "Larger", proseClass: "prose-xl" },
];

export const WIDTHS = [
  { value: "narrow", label: "Narrow", maxWidth: "58ch" },
  { value: "normal", label: "Normal", maxWidth: "68ch" },
  { value: "wide", label: "Wide", maxWidth: "80ch" },
];

export const THEMES = [
  { value: "dark", label: "Dark", icon: "pi-moon" },
  { value: "light", label: "Light", icon: "pi-sun" },
];

const DEFAULTS = { fontSize: "md", width: "normal", theme: "dark" };

function isValid(key, value, options) {
  return options.some((option) => option.value === value) ? value : DEFAULTS[key];
}

function sanitize(saved) {
  return {
    fontSize: isValid("fontSize", saved.fontSize, FONT_SIZES),
    width: isValid("width", saved.width, WIDTHS),
    theme: isValid("theme", saved.theme, THEMES),
  };
}

function load() {
  try {
    return sanitize(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "{}"));
  } catch {
    // Storage blocked or corrupt: fall back to defaults for this visit.
    return { ...DEFAULTS };
  }
}

/** Dark is the stylesheet default; light is opted into with data-theme="light". */
function applyTheme(theme) {
  if (theme === "light") document.documentElement.setAttribute("data-theme", "light");
  else document.documentElement.removeAttribute("data-theme");
}

// One shared, persisted instance for the whole app.
const preferences = reactive(load());

// Signed-in readers' preferences are also saved to their account, so they follow them to
// other devices. `synced` is what the account holds; null when signed out or not yet known.
const { isSignedIn } = useAuth();
let synced = null;
let saveTimer = null;
const snapshot = (value) => JSON.stringify(sanitize(value));

function saveSoon() {
  clearTimeout(saveTimer);
  // Clicking through sizes shouldn't send a request per click.
  saveTimer = setTimeout(async () => {
    const value = snapshot(preferences);
    if (!isSignedIn.value || synced === null || value === synced) return;
    try {
      await savePreferences(JSON.parse(value));
      synced = value;
    } catch {
      // Still saved on this device; the next change tries again.
    }
  }, 600);
}

watch(
  preferences,
  (value) => {
    applyTheme(value.theme);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    } catch {
      // Not persisted (private mode, storage full); the choice still applies this visit.
    }
    saveSoon();
  },
  { deep: true },
);

// On sign-in the account's preferences win. An account without any adopts this device's.
watch(
  isSignedIn,
  async (signedIn) => {
    synced = null;
    if (!signedIn) return;
    try {
      const saved = await getPreferences();
      if (!isSignedIn.value) return;
      if (saved) {
        synced = snapshot(saved);
        Object.assign(preferences, sanitize(saved));
      } else {
        synced = "";
        saveSoon();
      }
    } catch {
      // Keep this device's preferences; syncing resumes after the next sign-in.
    }
  },
  { immediate: true },
);

/** Call before mounting so the saved theme is in place on first paint. */
export function applySavedTheme() {
  applyTheme(preferences.theme);
}

export function useReaderPreferences() {
  return preferences;
}
