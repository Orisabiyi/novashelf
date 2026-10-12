/** Primeicons class for each topic slug (see server Topics.cs). */
export const TOPIC_ICONS = {
  mars: "pi-globe",
  moon: "pi-moon",
  iss: "pi-building",
  launches: "pi-send",
  telescopes: "pi-eye",
  exoplanets: "pi-star",
  earth: "pi-cloud",
  "solar-system": "pi-sun",
};

const KIND_LABELS = {
  apod: { label: "Picture of the day", icon: "pi-star-fill" },
  image: { label: "NASA image", icon: "pi-image" },
  article: { label: "Space news", icon: "pi-megaphone" },
};

/**
 * The label and icon for a content item's tag pill: its topic when it has one,
 * otherwise what kind of content it is.
 *
 * @param {import("@/api/content").ContentItem} item
 * @returns {{ label: string, icon: string }}
 */
export function itemTag(item) {
  if (item.topic) {
    return { label: item.topic.name, icon: TOPIC_ICONS[item.topic.slug] ?? "pi-compass" };
  }
  return KIND_LABELS[item.kind] ?? KIND_LABELS.article;
}
