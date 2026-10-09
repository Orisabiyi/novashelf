<script setup>
import { computed, ref } from "vue";

const props = defineProps({
  id: {
    type: String,
    required: true,
  },
  label: {
    type: String,
    required: true,
  },
  type: {
    type: String,
    default: "text",
  },
  autocomplete: {
    type: String,
    default: "off",
  },
  /** Shown under the field when there's no error, e.g. "Optional". */
  hint: {
    type: String,
    default: "",
  },
  error: {
    type: String,
    default: "",
  },
});

const model = defineModel({ type: String, default: "" });

const isPassword = computed(() => props.type === "password");
const revealed = ref(false);
const inputType = computed(() => (isPassword.value && revealed.value ? "text" : props.type));
const describedBy = computed(() => (props.error || props.hint ? `${props.id}-note` : undefined));
</script>

<template>
  <div class="flex flex-col gap-1.5 text-left">
    <label :for="id" class="text-sm font-medium">{{ label }}</label>
    <div class="relative">
      <input
        :id="id"
        v-model="model"
        :type="inputType"
        :autocomplete="autocomplete"
        :aria-invalid="error ? 'true' : undefined"
        :aria-describedby="describedBy"
        :class="[
          'w-full rounded-xl border bg-page px-4 py-3 text-base placeholder:text-muted focus:outline-2 focus:outline-offset-1 focus:outline-link',
          error ? 'border-link' : 'border-line-strong',
          isPassword && 'pr-12',
        ]" />
      <button
        v-if="isPassword"
        type="button"
        class="absolute right-2 top-1/2 -translate-y-1/2 grid size-9 place-items-center rounded-lg text-muted hover:text-ink"
        :aria-label="revealed ? 'Hide password' : 'Show password'"
        :aria-pressed="revealed"
        @click="revealed = !revealed">
        <i :class="revealed ? 'pi pi-eye-slash' : 'pi pi-eye'" aria-hidden="true"></i>
      </button>
    </div>
    <p v-if="error" :id="`${id}-note`" class="text-sm text-link" role="alert">{{ error }}</p>
    <p v-else-if="hint" :id="`${id}-note`" class="text-sm text-muted">{{ hint }}</p>
  </div>
</template>
