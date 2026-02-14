import { describe, it, expect, beforeEach, vi } from 'vitest'
import InvoiceStore from './invoiceStore'
import agent from '../api/agent'
import { Invoice, InvoiceStatus } from '../models/invoice'

// Mock the agent
vi.mock('../api/agent', () => ({
  default: {
    Invoices: {
      create: vi.fn(),
      addParticipant: vi.fn(),
      details: vi.fn(),
    },
    Users: {
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

    // Setup mock users
    store.userRegistry.clear()
    store.userRegistry.set('1', 'Epi')
    store.userRegistry.set('2', 'JHattu')
    store.userRegistry.set('3', 'Leivo')
    store.userRegistry.set('4', 'Timo')
    store.userRegistry.set('5', 'Jaapu')
    store.userRegistry.set('6', 'Urpi')
    store.userRegistry.set('7', 'Zeip')
    store.userRegistry.set('8', 'Antti')
    store.userRegistry.set('9', 'Sakke')
    store.userRegistry.set('10', 'Lasse')
  })

  it('should add all usual suspects when none are participants', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: '',
      status: InvoiceStatus.Aktiivinen,
      expenseItems: [],
      participants: [],
      approvals: [],
      amount: 0,
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)
    // Mock loadUsers to do nothing as we manually set registry
    store.loadUsers = vi.fn().mockResolvedValue(undefined);

    await store.addUsualSuspects(invoiceId)

    // Should have called addParticipant for all 7 usual suspects
    expect(agent.Invoices.addParticipant).toHaveBeenCalledTimes(7)
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '1') // Epi
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '2') // JHattu
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '3') // Leivo
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '4') // Timo
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '5') // Jaapu
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '6') // Urpi
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '7') // Zeip
  })

  it('should skip usual suspects that are already participants', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: '',
      status: InvoiceStatus.Aktiivinen,
      expenseItems: [],
      participants: [
        { invoiceId, appUserId: '1', appUser: { id: '1', displayName: 'Epi', userName: 'epi', email: '' } }, // Epi already participant
        { invoiceId, appUserId: '3', appUser: { id: '3', displayName: 'Leivo', userName: 'leivo', email: '' } }, // Leivo already participant
      ],
      amount: 0,
      approvals: []
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)
    store.loadUsers = vi.fn().mockResolvedValue(undefined);

    await store.addUsualSuspects(invoiceId)

    // Should only add 5 (7 total - 2 already participants)
    expect(agent.Invoices.addParticipant).toHaveBeenCalledTimes(5)
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, '1') // Epi skipped
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, '3') // Leivo skipped
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '2') // JHattu added
    expect(agent.Invoices.addParticipant).toHaveBeenCalledWith(invoiceId, '4') // Timo added
  })

  it('should not add non-usual-suspects even if they exist in users', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: '',
      status: InvoiceStatus.Aktiivinen,
      expenseItems: [],
      participants: [],
      approvals: [],
      amount: 0,
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)
    store.loadUsers = vi.fn().mockResolvedValue(undefined);

    await store.addUsualSuspects(invoiceId)

    // Should NOT add Antti, Sakke, or Lasse (IDs 8, 9, 10)
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, '8')
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, '9')
    expect(agent.Invoices.addParticipant).not.toHaveBeenCalledWith(invoiceId, '10')
  })

  it('should handle invoice without participants array', async () => {
    const invoiceId = 'test-invoice-id'
    const invoice: Invoice = {
      id: invoiceId,
      lanNumber: 1,
      title: 'Test Invoice',
      description: 'Test',
      image: '',
      status: InvoiceStatus.Aktiivinen,
      expenseItems: [],
      participants: undefined as any, // No participants array
      amount: 0,
      approvals: []
    }

    store.invoiceRegistry.set(invoiceId, invoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)
    store.loadUsers = vi.fn().mockResolvedValue(undefined);

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

    // Setup mock users
    store.userRegistry.clear()
    store.userRegistry.set('1', 'Epi')
    store.userRegistry.set('2', 'JHattu')
    store.userRegistry.set('3', 'Leivo')
    store.userRegistry.set('4', 'Timo')
    store.userRegistry.set('5', 'Jaapu')
    store.userRegistry.set('6', 'Urpi')
    store.userRegistry.set('7', 'Zeip')
    store.loadUsers = vi.fn().mockResolvedValue(undefined);
  })

  it('should automatically add usual suspects after creating invoice', async () => {
    const newInvoice: Invoice = {
      id: '',
      lanNumber: 0,
      title: 'New Invoice',
      description: 'Test',
      image: '',
      status: InvoiceStatus.Aktiivinen,
      expenseItems: [],
      participants: [],
      approvals: [],
      amount: 0,
    }

    const createdInvoice: Invoice = {
      ...newInvoice,
      id: 'created-invoice-id',
      lanNumber: 1,
    }

    vi.mocked(agent.Invoices.create).mockResolvedValue(createdInvoice)
    vi.mocked(agent.Invoices.addParticipant).mockResolvedValue(undefined)
    vi.mocked(agent.Invoices.details).mockResolvedValue(createdInvoice)

    await store.createInvoice(newInvoice)

    // Should create invoice
    expect(agent.Invoices.create).toHaveBeenCalledOnce()

    // Should automatically add usual suspects
    expect(agent.Invoices.addParticipant).toHaveBeenCalledTimes(7)
  })
})

describe('InvoiceStore - canCreateInvoice', () => {
  let store: InvoiceStore

  beforeEach(() => {
    store = new InvoiceStore()
  })

  it('should return true when there are no invoices', () => {
    expect(store.canCreateInvoice).toBe(true)
  })

  it('should return true when all invoices are archived', () => {
    store.invoiceRegistry.set('1', { id: '1', status: InvoiceStatus.Arkistoitu } as any)
    store.invoiceRegistry.set('2', { id: '2', status: InvoiceStatus.Arkistoitu } as any)
    expect(store.canCreateInvoice).toBe(true)
  })

  it('should return true when an invoice is in payment', () => {
    store.invoiceRegistry.set('1', { id: '1', status: InvoiceStatus.Maksussa } as any)
    expect(store.canCreateInvoice).toBe(true)
  })

  it('should return false when an invoice is active', () => {
    store.invoiceRegistry.set('1', { id: '1', status: InvoiceStatus.Aktiivinen } as any)
    expect(store.canCreateInvoice).toBe(false)
  })

  it('should return false if any invoice is active, even if others are archived or in payment', () => {
    store.invoiceRegistry.set('1', { id: '1', status: InvoiceStatus.Arkistoitu } as any)
    store.invoiceRegistry.set('2', { id: '2', status: InvoiceStatus.Maksussa } as any)
    store.invoiceRegistry.set('3', { id: '3', status: InvoiceStatus.Aktiivinen } as any)
    expect(store.canCreateInvoice).toBe(false)
  })
})
