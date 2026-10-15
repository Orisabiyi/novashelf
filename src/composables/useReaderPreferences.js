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
    return { ...DEFAULTS };
  }
}

function applyTheme(theme) {
  if (theme === "light") document.documentElement.setAttribute("data-theme", "light");
  else document.documentElement.removeAttribute("data-theme");
}

const preferences = reactive(load());

const { isSignedIn } = useAuth();
let synced = null;
let saveTimer = null;
const snapshot = (value) => JSON.stringify(sanitize(value));

function saveSoon() {
  clearTimeout(saveTimer);
  saveTimer = setTimeout(async () => {
    const value = snapshot(preferences);
    if (!isSignedIn.value || synced === null || value === synced) return;
    try {
      await savePreferences(JSON.parse(value));
      synced = value;
    } catch {
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
    }
  },
  { immediate: true },
);

export function applySavedTheme() {
  applyTheme(preferences.theme);
}

export function useReaderPreferences() {
  return preferences;
}
