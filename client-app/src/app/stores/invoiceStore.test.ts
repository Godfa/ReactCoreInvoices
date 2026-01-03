import { describe, it, expect, beforeEach, vi } from 'vitest'
import InvoiceStore from './invoiceStore'
import agent from '../api/agent'
import { Invoice } from 'Invoices'

// Mock the agent
vi.mock('../api/agent', () => ({
  default: {
    Invoices: {
      create: vi.fn(),
      addParticipant: vi.fn(),
    },
    Creditors: {
      list: vi.fn(),
    },
  },
}))

// Mock react-toastify
vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

describe('InvoiceStore - addUsualSuspects', () => {
  let store: InvoiceStore

  beforeEach(() => {
    store = new InvoiceStore()
    vi.clearAllMocks()

    // Setup mock creditors
    store.creditorRegistry.clear()
    store.creditorRegistry.set(1, 'Epi')
    store.creditorRegistry.set(2, 'JHattu')
    store.creditorRegistry.set(3, 'Leivo')
    store.creditorRegistry.set(4, 'Timo')
    store.creditorRegistry.set(5, 'Jaapu')
    store.creditorRegistry.set(6, 'Urpi')
    store.creditorRegistry.set(7, 'Zeip')
    store.creditorRegistry.set(8, 'Antti')
    store.creditorRegistry.set(9, 'Sakke')
    store.creditorRegistry.set(10, 'Lasse')
  })

  it('should add all usual suspects when none are participants', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: null,
      expenseItems: [],
      participants: [],
      amount: 0,
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)

    await store.addUsualSuspects(invoiceId)

    // Should have called addParticipant for all 7 usual suspects
    expect(agent.Invoices.addParticipant).toHaveBeenCalledTimes(7)
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 1) // Epi
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 2) // JHattu
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 3) // Leivo
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 4) // Timo
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 5) // Jaapu
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 6) // Urpi
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 7) // Zeip
  })

  it('should skip usual suspects that are already participants', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: null,
      expenseItems: [],
      participants: [
        { invoiceId, creditorId: 1, creditor: { id: 1, name: 'Epi' } }, // Epi already participant
        { invoiceId, creditorId: 3, creditor: { id: 3, name: 'Leivo' } }, // Leivo already participant
      ],
      amount: 0,
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)

    await store.addUsualSuspects(invoiceId)

    // Should only add 5 (7 total - 2 already participants)
    expect(agent.Invoices.addParticipant).toHaveBeenCalledTimes(5)
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, 1) // Epi skipped
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, 3) // Leivo skipped
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 2) // JHattu added
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, 4) // Timo added
  })

  it('should not add non-usual-suspects even if they exist in creditors', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: null,
      expenseItems: [],
      participants: [],
      amount: 0,
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)

    await store.addUsualSuspects(invoiceId)

    // Should NOT add Antti, Sakke, or Lasse (IDs 8, 9, 10)
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, 8)
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, 9)
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, 10)
  })

  it('should handle invoice without participants array', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: null,
      expenseItems: [],
      participants: undefined as any, // No participants array
      amount: 0,
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)

    await store.addUsualSuspects(invoiceId)

    // Should add all 7 usual suspects (undefined participants defaults to empty array)
    expect(agent.Invoices.addParticipant).toHaveBeenCalledTimes(7)
  })
})

describe('InvoiceStore - createInvoice with auto-add usual suspects', () => {
  let store: InvoiceStore

  beforeEach(() => {
    store = new InvoiceStore()
    vi.clearAllMocks()

    // Setup mock creditors
    store.creditorRegistry.clear()
    store.creditorRegistry.set(1, 'Epi')
    store.creditorRegistry.set(2, 'JHattu')
    store.creditorRegistry.set(3, 'Leivo')
    store.creditorRegistry.set(4, 'Timo')
    store.creditorRegistry.set(5, 'Jaapu')
    store.creditorRegistry.set(6, 'Urpi')
    store.creditorRegistry.set(7, 'Zeip')
  })

  it('should automatically add usual suspects after creating invoice', async () => {
    const newInvoice: Invoice = {
      id: '',
      lanNumber: 0,
      title: 'New Invoice',
      description: 'Test',
      image: null,
      expenseItems: [],
      participants: [],
      amount: 0,
    }

    const createdInvoice: Invoice = {
      ...newInvoice,
      id: 'created-invoice-id',
      lanNumber: 1,
    }

    vi.mocked(agent.Invoices.create).mockResolvedValue(createdInvoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)

    await store.createInvoice(newInvoice)

    // Should create invoice
    expect(agent.Invoices.create).toHaveBeenCalledOnce()

    // Should automatically add usual suspects
    expect(agent.Invoices.addParticipant).toHaveBeenCalledTimes(7)
  })
})
