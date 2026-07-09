<script setup lang="ts">
const emit = defineEmits<{
  success: [role: string]
}>()

const { login, fetchMe } = useAuth()

const email    = ref('')
const password = ref('')
const error    = ref<string | null>(null)
const loading  = ref(false)

async function submit() {
  error.value   = null
  loading.value = true
  try {
    await login(email.value, password.value)
    const me = await fetchMe()
    emit('success', me.role)
  } catch (err: unknown) {
    const status = (err as { status?: number })?.status
    if (status === 401 || status === 403) {
      error.value = 'Email ou senha inválidos.'
    } else if (status === 423) {
      error.value = 'Conta bloqueada. Tente novamente mais tarde.'
    } else if (status === 429) {
      error.value = 'Muitas tentativas. Aguarde um momento.'
    } else {
      error.value = 'Erro ao fazer login. Tente novamente.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <form class="login-form" novalidate @submit.prevent="submit">
    <div class="login-form__field">
      <label for="lf-email" class="login-form__label">E-mail</label>
      <input
        id="lf-email"
        v-model="email"
        type="email"
        autocomplete="username"
        required
        class="login-form__input"
        :disabled="loading"
      />
    </div>

    <div class="login-form__field">
      <label for="lf-password" class="login-form__label">Senha</label>
      <input
        id="lf-password"
        v-model="password"
        type="password"
        autocomplete="current-password"
        required
        class="login-form__input"
        :disabled="loading"
      />
    </div>

    <p v-if="error" role="alert" class="login-form__error">{{ error }}</p>

    <UiButton type="submit" :disabled="loading" class="login-form__submit">
      {{ loading ? 'Entrando…' : 'Entrar' }}
    </UiButton>
  </form>
</template>

<style scoped>
.login-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.login-form__field {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.login-form__label {
  color: var(--op-color-muted);
  font-size: 0.875rem;
}

.login-form__input {
  background: var(--op-color-bg, var(--op-color-surface));
  border: 1px solid var(--op-color-border);
  border-radius: var(--op-radius);
  color: var(--op-color-text);
  font-family: var(--op-font-sans);
  font-size: 0.9375rem;
  padding: 0.5rem 0.75rem;
}

.login-form__input:focus {
  outline: 2px solid var(--op-color-primary);
  outline-offset: 1px;
}

.login-form__input:disabled {
  opacity: 0.55;
}

.login-form__error {
  color: #f87171;
  font-size: 0.875rem;
  margin: 0;
}

.login-form__submit {
  width: 100%;
}
</style>
