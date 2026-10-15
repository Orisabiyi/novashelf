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

export function itemTag(item) {
  if (item.topic) {
    return { label: item.topic.name, icon: TOPIC_ICONS[item.topic.slug] ?? "pi-compass" };
  }
  return KIND_LABELS[item.kind] ?? KIND_LABELS.article;
}
