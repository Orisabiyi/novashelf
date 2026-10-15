<script setup>
import { onBeforeUnmount, onMounted, ref } from "vue";

const props = defineProps({
  density: {
    type: Number,
    default: 0.00018,
  },
});

const canvas = ref(null);
let ctx = null;
let stars = [];
let frame = 0;
let resizeObserver = null;
let themeObserver = null;
let starColor = "#ffffff";
let size = { width: 0, height: 0 };
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

function seed(width, height) {
  const count = Math.round(width * height * props.density);
  stars = Array.from({ length: count }, () => ({
    x: Math.random() * width,
    y: Math.random() * height,
    radius: Math.random() * 1.1 + 0.25,
    alpha: Math.random() * 0.55 + 0.25,
    speed: Math.random() * 0.0015 + 0.0004, // twinkle rate, radians per ms
    phase: Math.random() * Math.PI * 2,
  }));
}

function draw(time) {
  ctx.clearRect(0, 0, size.width, size.height);
  ctx.fillStyle = starColor;
  for (const star of stars) {
    const twinkle = reducedMotion.matches
      ? 1
      : 0.55 + 0.45 * Math.sin(time * star.speed + star.phase);
    ctx.globalAlpha = star.alpha * twinkle;
    ctx.beginPath();
    ctx.arc(star.x, star.y, star.radius, 0, Math.PI * 2);
    ctx.fill();
  }
}

// Stars take the text colour, so they follow the theme. Read it once, not every frame.
function readColor() {
  starColor = getComputedStyle(canvas.value).color;
}

function resize() {
  const element = canvas.value;
  const ratio = Math.min(window.devicePixelRatio || 1, 2);
  const { width, height } = element.getBoundingClientRect();
  size = { width, height };
  element.width = Math.round(width * ratio);
  element.height = Math.round(height * ratio);
  ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
  seed(width, height);
  draw(performance.now());
}

function tick(time) {
  draw(time);
  frame = requestAnimationFrame(tick);
}

onMounted(() => {
  ctx = canvas.value.getContext("2d");
  readColor();
  themeObserver = new MutationObserver(() => {
    readColor();
    draw(performance.now());
  });
  themeObserver.observe(document.documentElement, { attributeFilter: ["data-theme"] });
  resizeObserver = new ResizeObserver(resize);
  resizeObserver.observe(canvas.value);
  if (!reducedMotion.matches) frame = requestAnimationFrame(tick);
});

onBeforeUnmount(() => {
  cancelAnimationFrame(frame);
  resizeObserver?.disconnect();
  themeObserver?.disconnect();
});
</script>

<template>
  <canvas
    ref="canvas"
    class="starfield absolute inset-0 size-full text-ink pointer-events-none"
    aria-hidden="true"></canvas>
</template>

<style>
/* Dark stars on a light page read as dust; keep them faint there. */
:root[data-theme="light"] .starfield {
  opacity: 0.35;
}
</style>
