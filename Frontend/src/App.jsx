
import { useCallback, useEffect, useRef, useState } from 'react'
import CreateOrderModal from './Components/CreateOrderModal'
import OrdersList from './Components/OrdersList'
import { getOrders, createOrder } from './Services/OrdersService'
import { createOrdersHubConnection } from './Services/OrdersHub'

function App() {
  const [orders, setOrders] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)

  const hubConnectionRef = useRef(null)
  const ordersRef = useRef([])
  const subscribedOrderIdsRef = useRef(new Set())
  const syncSubscriptionsRef = useRef(null)
  const syncQueueRef = useRef(Promise.resolve())

  const loadOrders = useCallback(async () => {
    setIsLoading(true)
    setError('')

    try {
      const result = await getOrders()
      const items = Array.isArray(result) ? result : result.items

      if (!Array.isArray(items)) {
        throw new Error(
          'La respuesta de GET /orders no tiene el formato esperado.',
        )
      }

      setOrders(items)
    } catch (err) {
      setError(err.message || 'No fue posible consultar los pedidos.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadOrders()
  }, [loadOrders])

  useEffect(() => {
    ordersRef.current = orders
    syncSubscriptionsRef.current?.()
  }, [orders])

  useEffect(() => {
    const connection = createOrdersHubConnection()
    let isDisposed = false

    hubConnectionRef.current = connection

    async function synchronizeSubscriptions() {
      if (isDisposed || connection.state !== 'Connected') {
        return
      }

      const pendingIds = new Set(
        ordersRef.current
          .filter((order) => order.status === 'Pending' && order.id)
          .map((order) => order.id),
      )

      for (const orderId of [...subscribedOrderIdsRef.current]) {
        if (pendingIds.has(orderId)) {
          continue
        }

        await connection.invoke('UnsubscribeFromOrder', orderId)
        subscribedOrderIdsRef.current.delete(orderId)
      }

      for (const orderId of pendingIds) {
        if (subscribedOrderIdsRef.current.has(orderId)) {
          continue
        }

        await connection.invoke('SubscribeToOrder', orderId)
        subscribedOrderIdsRef.current.add(orderId)
      }
    }

    function queueSubscriptionSync() {
      syncQueueRef.current = syncQueueRef.current
        .catch(() => {})
        .then(synchronizeSubscriptions)
        .catch((err) => {
          if (!isDisposed) {
            console.error(
              'Error sincronizando suscripciones de SignalR:',
              err,
            )
          }
        })
    }

    syncSubscriptionsRef.current = queueSubscriptionSync

    function handleOrderStatusChanged(event) {
      const updatedOrders = ordersRef.current.map((order) =>
        order.id === event.orderId
          ? { ...order, status: event.status }
          : order,
      )

      ordersRef.current = updatedOrders
      setOrders(updatedOrders)

      queueSubscriptionSync()
    }

    connection.on('OrderStatusChanged', handleOrderStatusChanged)

    connection.onreconnected(() => {
      subscribedOrderIdsRef.current.clear()
      queueSubscriptionSync()
    })

    connection
      .start()
      .then(() => {
        if (!isDisposed) {
          queueSubscriptionSync()
        }
      })
      .catch((err) => {
        if (!isDisposed) {
          console.error('Error al conectar con Orders Hub:', err)
        }
      })

    return () => {
      isDisposed = true
      syncSubscriptionsRef.current = null
      subscribedOrderIdsRef.current.clear()

      connection.off(
        'OrderStatusChanged',
        handleOrderStatusChanged,
      )

      hubConnectionRef.current = null
      void connection.stop()
    }
  }, [])

  async function handleCreateOrder(orderData) {
    await createOrder(orderData)
    setIsModalOpen(false)
    await loadOrders()
  }

  return (
    <main className="min-h-screen bg-neutral-50 text-neutral-900">
      <header className="border-b border-neutral-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-5 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <div className="flex h-25 w-25 shrink-0 items-center justify-center">
              <img src="/logo.png" alt="OrderFlow" className="h-full w-full object-contain" />
            </div>

            <div>
              <h1 className="text-xl font-bold">OrderFlow</h1>
              <p className="text-sm text-neutral-500">
                Gestión de pedidos
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={() => setIsModalOpen(true)}
            className="rounded-lg bg-brand-500 px-4 py-2.5 font-semibold text-neutral-900 transition-colors hover:bg-brand-600"
          >
            <span className="mr-1" aria-hidden="true">+</span>
            Crear pedido
          </button>
        </div>
      </header>

      <section className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
        <div className="mb-6">
          <h2 className="text-2xl font-bold">Pedidos</h2>
          <p className="mt-1 text-neutral-500">
            Consulta y seguimiento de los pedidos registrados.
          </p>
        </div>

        <OrdersList
          orders={orders}
          isLoading={isLoading}
          error={error}
        />
      </section>

      {isModalOpen && (
        <CreateOrderModal
          onClose={() => setIsModalOpen(false)}
          onSubmit={handleCreateOrder}
        />
      )}
    </main>
  )
}

export default App
