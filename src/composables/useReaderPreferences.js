import { reactive, watch } from "vue";

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

function load() {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? "{}");
    return {
      fontSize: isValid("fontSize", saved.fontSize, FONT_SIZES),
      width: isValid("width", saved.width, WIDTHS),
      theme: isValid("theme", saved.theme, THEMES),
    };
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

watch(
  preferences,
  (value) => {
    applyTheme(value.theme);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    } catch {
      // Not persisted (private mode, storage full); the choice still applies this visit.
    }
  },
  { deep: true },
);

/** Call before mounting so the saved theme is in place on first paint. */
export function applySavedTheme() {
  applyTheme(preferences.theme);
}

export function useReaderPreferences() {
  return preferences;
}
