<script setup>
import { ref } from "vue";

const email = ref("");
</script>

<template>
  <div>
    <h3>Join the shelf</h3>
    <p>
      Create an account to save articles, track what you've read, and explore
      space at your own pace.
    </p>

    <form>
      <input type="email" placeholder="Email" v-model="email" class="block" />
      <button>Sign Up</button>
    </form>
  </div>
</template>
