<script setup>
import Navbar from "@/components/shared/Navbar.vue";
import { RouterView } from "vue-router";
</script>

<template>
  <Navbar className="fixed top-0 left-0 w-full" />
  <RouterView />
</template>
