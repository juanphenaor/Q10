import { useEffect, useRef, useState } from 'react'

const MAX_QUANTITY = 100
const MAX_CLIENT_NAME_LENGTH = 200

function CreateOrderModal({ onClose, onSubmit }) {
  const [clientName, setClientName] = useState('')
  const [items, setItems] = useState([{ id: 0, sku: '', quantity: '1' }])
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const nextItemId = useRef(1)
  const submitting = useRef(false)

  useEffect(() => {
    function handleKeyDown(event) {
      if (event.key === 'Escape' && !submitting.current) onClose()
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  function updateItem(id, field, value) {
    setItems((currentItems) =>
      currentItems.map((item) =>
        item.id === id ? { ...item, [field]: value } : item,
      ),
    )
  }

  function addItem() {
    const id = nextItemId.current++
    setItems((currentItems) => [
      ...currentItems,
      { id, sku: '', quantity: '1' },
    ])
  }

  async function handleSubmit(event) {
    event.preventDefault()
    if (submitting.current) return
    setError('')

    const normalizedClientName = clientName.trim()
    if (!normalizedClientName) {
      setError('El nombre del cliente no puede estar vacío ni en blanco.')
      return
    }

    if (normalizedClientName.length > MAX_CLIENT_NAME_LENGTH) {
      setError(`El nombre del cliente no puede superar ${MAX_CLIENT_NAME_LENGTH} caracteres.`)
      return
    }

    if (items.length === 0) {
      setError('Agrega al menos un artículo.')
      return
    }

    const groupedItems = new Map()

    for (const [index, item] of items.entries()) {
      const sku = item.sku.trim()
      const quantity = Number(item.quantity)

      if (!sku) {
        setError(`El SKU del artículo ${index + 1} no puede estar vacío ni en blanco.`)
        return
      }

      if (!Number.isInteger(quantity) || quantity < 1 || quantity > MAX_QUANTITY) {
        setError(`La cantidad del artículo ${index + 1} debe ser un entero entre 1 y ${MAX_QUANTITY}.`)
        return
      }

      const totalQuantity = (groupedItems.get(sku) ?? 0) + quantity

      if (totalQuantity > MAX_QUANTITY) {
        setError(`La cantidad total del SKU "${sku}" no puede superar ${MAX_QUANTITY}.`)
        return
      }

      groupedItems.set(sku, totalQuantity)
    }

    submitting.current = true
    setIsSubmitting(true)

    try {
      await onSubmit({
        clientName: normalizedClientName,
        items: Array.from(groupedItems, ([sku, quantity]) => ({ sku, quantity })),
      })
    } catch (err) {
      setError(err.message || 'No fue posible crear el pedido.')
    } finally {
      submitting.current = false
      setIsSubmitting(false)
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-neutral-900/60 p-4"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !submitting.current) onClose()
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="create-order-title"
        className="max-h-[calc(100dvh-2rem)] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl sm:p-8"
      >
        <div className="mb-6 flex items-start justify-between gap-4">
          <div>
            <h2 id="create-order-title" className="text-xl font-bold text-neutral-900">
              Crear pedido
            </h2>
            <p className="mt-1 text-sm text-neutral-500">
              Ingresa los datos del nuevo pedido.
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar formulario"
            className="rounded-lg p-2 text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-900 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              className="h-5 w-5"
              aria-hidden="true"
            >
              <path d="M18 6 6 18M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
          <fieldset disabled={isSubmitting} className="space-y-5">
            <div>
              <label htmlFor="clientName" className="mb-1.5 block text-sm font-medium text-neutral-700">
                Nombre del cliente
              </label>
              <input
                id="clientName"
                name="clientName"
                value={clientName}
                onChange={(event) => setClientName(event.target.value)}
                maxLength={MAX_CLIENT_NAME_LENGTH}
                required
                autoFocus
                className="w-full rounded-lg border border-neutral-200 px-3 py-2.5 text-neutral-900 outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
                placeholder="Ej. María Pérez"
              />
            </div>

            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-neutral-900">Artículos</h3>
              <p className="text-xs text-neutral-500">
                Agrega uno o varios artículos. Los SKU iguales se agrupan y sus cantidades se suman.
              </p>
              {items.map((item, index) => (
                <fieldset key={item.id} className="rounded-lg border border-neutral-200 p-3">
                  <legend className="px-1 text-sm font-medium text-neutral-700">
                    Artículo {index + 1}
                  </legend>
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div>
                      <label htmlFor={`sku-${item.id}`} className="mb-1.5 block text-sm font-medium text-neutral-700">
                        SKU del producto
                      </label>
                      <input
                        id={`sku-${item.id}`}
                        name={`sku-${item.id}`}
                        value={item.sku}
                        onChange={(event) => updateItem(item.id, 'sku', event.target.value)}
                        required
                        className="w-full rounded-lg border border-neutral-200 px-3 py-2.5 text-neutral-900 outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
                        placeholder="Ej. SKU-001"
                      />
                    </div>
                    <div>
                      <label htmlFor={`quantity-${item.id}`} className="mb-1.5 block text-sm font-medium text-neutral-700">
                        Cantidad
                      </label>
                      <input
                        id={`quantity-${item.id}`}
                        name={`quantity-${item.id}`}
                        type="number"
                        min="1"
                        max={MAX_QUANTITY}
                        step="1"
                        value={item.quantity}
                        onChange={(event) => updateItem(item.id, 'quantity', event.target.value)}
                        required
                        className="w-full rounded-lg border border-neutral-200 px-3 py-2.5 text-neutral-900 outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
                      />
                    </div>
                  </div>
                  <button
                    type="button"
                    disabled={items.length === 1}
                    onClick={() => setItems((currentItems) =>
                      currentItems.filter((currentItem) => currentItem.id !== item.id),
                    )}
                    aria-label={`Quitar artículo ${index + 1}`}
                    className="mt-3 text-sm font-medium text-status-rejected disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    Quitar artículo
                  </button>
                </fieldset>
              ))}

              <button
                type="button"
                onClick={addItem}
                className="rounded-lg border border-neutral-200 px-4 py-2.5 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-50"
              >
                Agregar artículo
              </button>
              <p className="text-xs text-neutral-500">
                Las cantidades deben ser números enteros mayores que 0.
              </p>
            </div>

            {error && (
              <p role="alert" className="rounded-lg bg-status-rejected-bg px-3 py-2.5 text-sm text-status-rejected">
                {error}
              </p>
            )}

            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={onClose}
                className="rounded-lg border border-neutral-200 px-4 py-2.5 font-medium text-neutral-700 transition-colors hover:bg-neutral-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                className="rounded-lg bg-brand-500 px-4 py-2.5 font-semibold text-neutral-900 transition-colors hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isSubmitting ? 'Creando pedido...' : 'Crear pedido'}
              </button>
            </div>
          </fieldset>
        </form>
      </section>
    </div>
  )
}

export default CreateOrderModal