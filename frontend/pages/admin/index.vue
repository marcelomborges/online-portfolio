<script setup lang="ts">
import type { MeResponse } from '~/types/auth'

definePageMeta({ middleware: 'auth', layout: 'app' })

const { fetchMe } = useAuth()
const { data: me, error } = await useAsyncData<MeResponse>('admin-me', () => fetchMe())

if (error.value) {
  await navigateTo('/login')
}

const roleLabel: Record<string, string> = {
  PlatformAdmin: 'Administrador da plataforma',
  Owner: 'Proprietário',
  Editor: 'Editor',
}
</script>

<template>
  <div v-if="me" class="admin-page">
    <UiCard :aria-label="`Sessão de ${me.email}`">
      <template #title>
        {{ me.tenant?.displayName ?? 'Plataforma' }}
      </template>
      <UiKeyValueList
        :items="[
          { label: 'E-mail', value: me.email },
          { label: 'Perfil', value: roleLabel[me.role] ?? me.role },
          ...(me.tenant ? [{ label: 'Tenant', value: me.tenant.slug }] : []),
        ]"
      />
    </UiCard>

    <div v-if="me.role === 'PlatformAdmin'" class="admin-page__nav">
      <NuxtLink to="/platform/tenants" class="admin-page__link">
        Gerenciar tenants e usuários →
      </NuxtLink>
    </div>

    <UiCard v-else aria-label="Dashboard">
      <template #title>Dashboard</template>
      <p class="admin-page__placeholder">Dashboard em construção.</p>
    </UiCard>
  </div>
</template>

<style scoped>
.admin-page {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.admin-page__link {
  color: var(--op-color-primary);
  font-size: 0.9375rem;
  text-decoration: none;
}

.admin-page__link:hover {
  color: var(--op-color-primary-hover);
  text-decoration: underline;
}

.admin-page__placeholder {
  color: var(--op-color-muted);
  font-size: 0.9375rem;
  margin: 0;
}
</style>
