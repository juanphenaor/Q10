import React from 'react'

const statusStyles = {
  Pending: 'bg-status-pending-bg text-status-pending',
  Confirmed: 'bg-status-confirmed-bg text-status-confirmed',
  Rejected: 'bg-status-rejected-bg text-status-rejected',
}

const statusLabels = {
  Pending: 'Pendiente',
  Confirmed: 'Confirmado',
  Rejected: 'Rechazado',
}

const formatUnitPrice = (unitPrice) =>
  Number(unitPrice).toLocaleString('es-CO')

function OrdersList({ orders, isLoading = false, error = '' }) {
  const [searchTerm, setSearchTerm] = React.useState('')

  const filteredOrders = orders.filter((order) => {
    const searchValue = searchTerm.trim().toLowerCase()

    return (
      String(order.id ?? '').toLowerCase().includes(searchValue) ||
      String(order.clientName ?? '').toLowerCase().includes(searchValue) ||
      order.items.some((item) =>
        String(item.sku ?? '').toLowerCase().includes(searchValue),
      )
    )
  })

  if (isLoading) {
    return (
      <div className="rounded-2xl border border-neutral-200 bg-white px-5 py-12 text-center">
        <p className="text-sm text-neutral-500">Cargando pedidos...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div
        role="alert"
        className="rounded-2xl border border-status-rejected-bg bg-white px-5 py-8 text-center"
      >
        <p className="font-medium text-status-rejected">
          No fue posible cargar los pedidos.
        </p>
        <p className="mt-1 text-sm text-neutral-500">{error}</p>
      </div>
    )
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-neutral-200 bg-white shadow-sm">
      <div className="flex flex-col gap-3 border-b border-neutral-200 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="font-semibold">Todos los pedidos</h3>
          <p className="mt-1 text-sm text-neutral-500">
            {filteredOrders.length} de {orders.length} pedidos
          </p>
        </div>

        <label className="relative block sm:w-72">
          <span className="sr-only">Buscar pedidos</span>
          <input
            type="search"
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
            placeholder="Buscar pedido, cliente o SKU..."
            className="w-full rounded-lg border border-neutral-200 px-3 py-2.5 text-sm text-neutral-900 outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
          />
        </label>
      </div>

      {filteredOrders.length === 0 ? (
        <div className="px-5 py-12 text-center">
          <p className="font-medium text-neutral-700">
            {orders.length === 0
              ? 'Todavía no hay pedidos.'
              : 'No se encontraron pedidos.'}
          </p>
          <p className="mt-1 text-sm text-neutral-500">
            {orders.length === 0
              ? 'Crea tu primer pedido para comenzar.'
              : 'Prueba con otro término de búsqueda.'}
          </p>
        </div>
      ) : (
        <>
          <div className="divide-y divide-neutral-200 md:hidden">
            {filteredOrders.map((order) => (
              <article key={order.id} className="space-y-3 p-5">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="break-words font-semibold">{order.id}</p>
                    <p className="mt-1 text-xs text-neutral-500">
                      Cliente: {order.clientName}
                    </p>
                  </div>

                  <span
                    className={`shrink-0 rounded-full px-3 py-1 text-xs font-medium ${
                      statusStyles[order.status] ??
                      'bg-neutral-100 text-neutral-700'
                    }`}
                  >
                    {statusLabels[order.status] ?? order.status}
                  </span>
                </div>

                <ul className="space-y-1 text-sm text-neutral-700">
                  {order.items.map((item) => (
                    <li key={item.id}>
                      {item.sku} · Cantidad: {item.quantity} · Precio
                      unitario: {formatUnitPrice(item.unitPrice)}
                    </li>
                  ))}
                </ul>
              </article>
            ))}
          </div>

          <div className="hidden overflow-x-auto md:block">
            <table className="w-full text-left text-sm">
              <thead className="bg-neutral-50 text-neutral-500">
                <tr>
                  <th className="px-5 py-3 font-medium">Pedido</th>
                  <th className="px-5 py-3 font-medium">Cliente</th>
                  <th className="px-5 py-3 font-medium">Artículos</th>
                  <th className="px-5 py-3 font-medium">Estado</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-neutral-200">
                {filteredOrders.map((order) => (
                  <tr
                    key={order.id}
                    className="transition-colors hover:bg-neutral-50"
                  >
                    <td className="whitespace-nowrap px-5 py-4 font-medium">
                      {order.id}
                    </td>
                    <td className="px-5 py-4">{order.clientName}</td>
                    <td className="px-5 py-4">
                      <ul className="space-y-1">
                        {order.items.map((item) => (
                          <li key={item.id}>
                            {item.sku} · Cantidad: {item.quantity} · Precio
                            unitario: {formatUnitPrice(item.unitPrice)}
                          </li>
                        ))}
                      </ul>
                    </td>
                    <td className="px-5 py-4">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-medium ${
                          statusStyles[order.status] ??
                          'bg-neutral-100 text-neutral-700'
                        }`}
                      >
                        {statusLabels[order.status] ?? order.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}

export default OrdersList