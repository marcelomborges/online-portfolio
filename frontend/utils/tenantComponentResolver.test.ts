import { describe, expect, it } from 'vitest'
import { listTenantUiSlugs } from './tenantComponentResolver'

describe('tenantComponentResolver', () => {
  it('listTenantUiSlugs includes seed tenants from repo folders', () => {
    expect(listTenantUiSlugs()).toEqual(expect.arrayContaining(['ana', 'joao']))
  })
})
